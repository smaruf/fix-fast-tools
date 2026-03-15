#!/usr/bin/env python3
"""
replicator.py — CSE FAST Market Data Replay Tool (Python version)

Replays historical FAST market data messages from a MongoDB collection,
decoding binary payloads and structured JSON fields for analysis or
testing purposes.

Requirements:
    pip install pymongo

Usage (library):
    from replicator import FastReplicator
    r = FastReplicator("mongodb://localhost:27017", "OmsTradingApi_CSE_FAST_DB")
    r.connect()
    r.replay(start, end)
    r.close()
"""

from __future__ import annotations

import base64
import json
from datetime import datetime, timezone
from typing import Dict, List, Optional, Tuple


# ---------------------------------------------------------------------------
# FAST binary decoder (stop-bit encoding)
# ---------------------------------------------------------------------------

#: Template IDs → human-readable names (CSE FAST template set)
TEMPLATE_NAMES: Dict[int, str] = {
    1:  "Logon",
    2:  "Logout",
    3:  "Heartbeat",
    4:  "SecurityDefinitionRequest",
    7:  "MarketDataRequest",
    9:  "ApplicationMessageRequest",
    14: "SecurityDefinition",
    15: "SecurityStatus",
    18: "MDSnapshot",
    20: "MDIncrementalRefresh",
    24: "News",
    25: "MarketDataRequestReject",
    26: "BusinessMessageReject",
    27: "ApplicationMessageRequestAck",
    28: "ApplicationMessageReport",
}


def _read_stop_bit_uint(data: bytes, offset: int) -> Tuple[int, int]:
    """Read a FAST stop-bit-encoded unsigned integer from *data* at *offset*.

    Returns ``(value, bytes_read)``.  Returns ``(0, 0)`` when no valid
    integer can be decoded (e.g. offset beyond end of buffer).
    """
    value = 0
    read = 0
    for i in range(offset, min(offset + 5, len(data))):
        b = data[i]
        value = (value << 7) | (b & 0x7F)
        read += 1
        if b & 0x80:           # stop bit set → last byte
            return value, read
    return 0, 0


def decode_binary(data: bytes, template_map: Optional[Dict[int, str]] = None) -> Dict:
    """Decode a raw FAST binary payload.

    Returns a dict with keys:

    * ``hex``           – upper-case hex string of the raw bytes
    * ``template_id``   – integer template ID (or ``None``)
    * ``template_name`` – resolved name from *template_map* (or ``None``)
    * ``strings``       – list of printable ASCII runs ≥ 3 chars
    * ``integers``      – first ≤ 10 stop-bit integers decoded from head
    """
    tmap = {**TEMPLATE_NAMES, **(template_map or {})}
    result: Dict = {
        "hex": data.hex().upper(),
        "template_id": None,
        "template_name": None,
        "strings": [],
        "integers": [],
    }

    if not data:
        return result

    # First stop-bit uint is the FAST template ID
    tid, read = _read_stop_bit_uint(data, 0)
    if read > 0:
        result["template_id"] = tid
        result["template_name"] = tmap.get(tid, f"Unknown({tid})")

    # Extract printable ASCII runs
    run: List[str] = []
    for b in data:
        if 32 <= b <= 126:
            run.append(chr(b))
        else:
            if len(run) >= 3:
                result["strings"].append("".join(run))
            run = []
    if len(run) >= 3:
        result["strings"].append("".join(run))

    # Extract first ≤ 10 stop-bit integers
    pos = 0
    for _ in range(10):
        if pos >= len(data):
            break
        val, rd = _read_stop_bit_uint(data, pos)
        if rd == 0:
            break
        result["integers"].append(val)
        pos += rd

    return result


# ---------------------------------------------------------------------------
# Single-message processing
# ---------------------------------------------------------------------------

def process_message(msg: Dict, template_map: Optional[Dict[int, str]] = None) -> Dict:
    """Process one ``FastIncomingMessage`` document from MongoDB.

    Returns a dict with:

    * ``success`` (bool)
    * ``error``   (str | None) – populated only on failure
    * ``binary``  (dict | None) – result of :func:`decode_binary`
    * ``fields``  (dict | None) – parsed ``MsgText`` JSON
    """
    msg_name = msg.get("MsgName") or "Unknown"
    msg_text = msg.get("MsgText") or ""
    raw_msg  = msg.get("RawMsg")

    # Guard: nothing to decode
    if not msg_text and not raw_msg:
        return {"success": False, "error": f"{msg_name}: empty message (no MsgText, no RawMsg)"}

    if isinstance(msg_text, str):
        if msg_text.startswith("RAW_HEX:"):
            return {"success": False, "error": f"{msg_name}: RAW_HEX binary stub – not deserializable"}
        if msg_text.startswith("RAW_EMPTY"):
            return {"success": False, "error": f"{msg_name}: RAW_EMPTY packet"}

    result: Dict = {"success": True, "error": None, "binary": None, "fields": None}

    # 1. Decode binary payload when present
    if raw_msg:
        raw_bytes: Optional[bytes] = None
        if isinstance(raw_msg, bytes):
            raw_bytes = raw_msg
        elif isinstance(raw_msg, str):
            try:
                raw_bytes = base64.b64decode(raw_msg)
            except Exception:
                pass

        if raw_bytes:
            try:
                result["binary"] = decode_binary(raw_bytes, template_map)
            except Exception:
                pass  # non-fatal, fall through to JSON path

    # 2. Parse structured JSON fields from MsgText
    if msg_text:
        try:
            result["fields"] = json.loads(msg_text)
        except Exception:
            pass

    # At least one decode path must have succeeded
    if result["binary"] is None and result["fields"] is None:
        return {"success": False, "error": f"Could not decode {msg_name}"}

    return result


# ---------------------------------------------------------------------------
# Replicator class
# ---------------------------------------------------------------------------

class FastReplicator:
    """Replays historical FAST market data from a MongoDB collection.

    Parameters
    ----------
    mongo_uri:
        Full MongoDB connection URI, e.g. ``"mongodb://localhost:27017"``.
    db_name:
        Name of the database, e.g. ``"OmsTradingApi_CSE_FAST_DB"``.
    collection_name:
        MongoDB collection holding ``FastIncomingMessage`` documents.
    template_map:
        Optional dict of ``{template_id: name}`` to override/extend the
        built-in :data:`TEMPLATE_NAMES`.

    Examples
    --------
    >>> r = FastReplicator("mongodb://localhost:27017", "MyDb")
    >>> r.connect()
    >>> r.print_status()
    >>> from datetime import datetime, timezone
    >>> start = datetime(2024, 3, 13, tzinfo=timezone.utc)
    >>> r.replay(start, start.replace(hour=23, minute=59))
    >>> r.close()
    """

    def __init__(
        self,
        mongo_uri: str = "mongodb://localhost:27017",
        db_name: str = "OmsTradingApi_CSE_FAST_DB",
        collection_name: str = "FastIncomingMessages",
        template_map: Optional[Dict[int, str]] = None,
    ) -> None:
        self.mongo_uri = mongo_uri
        self.db_name = db_name
        self.collection_name = collection_name
        self.template_map = template_map or {}
        self._client = None
        self._collection = None

    # ------------------------------------------------------------------
    # Connection management
    # ------------------------------------------------------------------

    def connect(self, timeout_ms: int = 5000) -> None:
        """Open a connection to MongoDB and verify it is reachable."""
        try:
            from pymongo import MongoClient  # type: ignore[import]
        except ImportError as exc:
            raise ImportError(
                "pymongo is required: pip install pymongo"
            ) from exc

        self._client = MongoClient(self.mongo_uri, serverSelectionTimeoutMS=timeout_ms)
        # Probe connectivity (raises if server is unreachable)
        self._client.server_info()
        db = self._client[self.db_name]
        self._collection = db[self.collection_name]

    def close(self) -> None:
        """Close the MongoDB connection."""
        if self._client is not None:
            self._client.close()
            self._client = None
            self._collection = None

    # ------------------------------------------------------------------
    # Database statistics
    # ------------------------------------------------------------------

    def get_status(self) -> Dict:
        """Return a status dictionary describing the collection contents."""
        self._require_connection()
        col = self._collection

        total = col.count_documents({})
        status: Dict = {"total": total, "earliest": None, "latest": None, "message_types": []}

        if total == 0:
            return status

        earliest = col.find_one({}, sort=[("SendingDateTimeUtc", 1)])
        latest   = col.find_one({}, sort=[("SendingDateTimeUtc", -1)])
        status["earliest"] = earliest.get("SendingDateTimeUtc") if earliest else None
        status["latest"]   = latest.get("SendingDateTimeUtc")   if latest   else None

        pipeline = [
            {"$group": {"_id": "$MsgName", "count": {"$sum": 1}}},
            {"$sort":  {"_id": 1}},
        ]
        status["message_types"] = list(col.aggregate(pipeline))
        return status

    def print_status(self) -> None:
        """Print a human-readable database status to stdout."""
        s = self.get_status()
        print(f"  Total messages : {s['total']:,}")
        if s["total"] == 0:
            print("  WARNING: Collection is empty – no messages to replay.")
            return
        if s["earliest"] and s["latest"]:
            fmt = "%Y-%m-%d %H:%M:%S"
            print(f"  Date range     : {s['earliest'].strftime(fmt)} UTC  →  {s['latest'].strftime(fmt)} UTC")
        print(f"  Message types  : {len(s['message_types'])}")
        for mt in s["message_types"]:
            name  = mt.get("_id") or "Unknown"
            count = mt.get("count", 0)
            print(f"    {name:<40}  {count:>8,}")

    # ------------------------------------------------------------------
    # Query helpers
    # ------------------------------------------------------------------

    def get_messages(self, start: datetime, end: datetime) -> List[Dict]:
        """Return all messages whose ``SendingDateTimeUtc`` is in ``[start, end)``."""
        self._require_connection()
        query = {"SendingDateTimeUtc": {"$gte": start, "$lt": end}}
        return list(self._collection.find(query).sort("SendingDateTimeUtc", 1))

    # ------------------------------------------------------------------
    # Replay
    # ------------------------------------------------------------------

    def replay(
        self,
        start: datetime,
        end: datetime,
        progress_interval: int = 100,
        verbose: bool = False,
    ) -> Dict:
        """Replay all messages in the date window ``[start, end)``.

        Parameters
        ----------
        start / end:
            UTC-aware or naïve ``datetime`` objects defining the window.
        progress_interval:
            Print a progress line every *N* successfully processed messages.
        verbose:
            When ``True`` print per-message details for MDIncrementalRefresh.

        Returns
        -------
        dict
            ``{"total": int, "processed": int, "errors": int,
               "error_summary": {str: int}}``
        """
        messages  = self.get_messages(start, end)
        total     = len(messages)
        processed = 0
        errors    = 0
        error_summary: Dict[str, int] = {}
        error_examples: Dict[str, str] = {}

        print(f"Found {total:,} messages to replay")
        print()

        for msg in messages:
            result = process_message(msg, self.template_map)
            ts_str = ""
            ts_val = msg.get("SendingDateTimeUtc")
            if isinstance(ts_val, datetime):
                ts_str = ts_val.strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]

            if result["success"]:
                processed += 1

                if verbose and msg.get("MsgName") == "MDIncrementalRefresh":
                    flds  = result.get("fields") or {}
                    items = flds.get("IncRefMDEntries", {}).get("Items") if isinstance(flds, dict) else None
                    count = len(items) if isinstance(items, list) else 0
                    print(f"  \u2713 MDIncrementalRefresh  {ts_str}  ch={msg.get('Channel')}  entries={count}")

                if processed % progress_interval == 0:
                    print(f"  Processed {processed:,} messages...")
            else:
                errors += 1
                key = result.get("error") or "Unknown error"
                error_summary[key] = error_summary.get(key, 0) + 1
                if key not in error_examples:
                    ch = msg.get("Channel", "")
                    error_examples[key] = f"{ts_str}  ch={ch}"

        print()
        print("Summary:")
        print(f"  Total messages       : {total:,}")
        print(f"  Successfully decoded : {processed:,}")
        print(f"  Errors               : {errors:,}")

        if error_summary:
            print()
            print("Error Breakdown:")
            for key, cnt in sorted(error_summary.items(), key=lambda x: -x[1]):
                print(f"  {cnt:6}x  {key}")
                if key in error_examples:
                    print(f"          first: {error_examples[key]}")

        return {
            "total":         total,
            "processed":     processed,
            "errors":        errors,
            "error_summary": error_summary,
        }

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------

    def _require_connection(self) -> None:
        if self._collection is None:
            raise RuntimeError("Not connected. Call connect() first.")
