#!/usr/bin/env python3
"""
run_replicator.py — Standalone launcher for the FAST Market Data Replicator.

Supports two backends:
  • Python  – uses replicator.py + pymongo (no .NET required)
  • .NET    – builds and runs the C# project (requires .NET 8 SDK)

Interactive mode (default) prompts for MongoDB URI, database, start date,
and replay duration.  All values can also be supplied via CLI flags for
non-interactive / scripted use.

Usage
-----
  python run_replicator.py                          # fully interactive
  python run_replicator.py --python                 # force Python backend
  python run_replicator.py --dotnet                 # force .NET backend
  python run_replicator.py --start 2024-03-13       # skip start-date prompt
  python run_replicator.py --start 2024-03-13 --hours 8
  python run_replicator.py --uri mongodb://host:27017 --db MyDb
  python run_replicator.py --check-deps             # report available deps
"""

from __future__ import annotations

import argparse
import os
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path


# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

SCRIPT_DIR    = Path(__file__).resolve().parent
REPO_ROOT     = SCRIPT_DIR.parent
CSPROJ        = SCRIPT_DIR / "EcoSoftBD.Oms.Fast.ReplyFASTMarketData.csproj"
REPLICATOR_PY = SCRIPT_DIR / "replicator.py"

DEFAULT_MONGO_URI = "mongodb://localhost:27017"
DEFAULT_DB_NAME   = "OmsTradingApi_CSE_FAST_DB"
DEFAULT_COLLECTION = "FastIncomingMessages"


# ---------------------------------------------------------------------------
# Dependency probes
# ---------------------------------------------------------------------------

def _has_pymongo() -> bool:
    try:
        import pymongo  # noqa: F401
        return True
    except ImportError:
        return False


def _has_dotnet() -> bool:
    try:
        r = subprocess.run(["dotnet", "--version"], capture_output=True, check=True)
        ver = r.stdout.decode().strip()
        # Require .NET 8+
        major = int(ver.split(".")[0])
        return major >= 8
    except Exception:
        return False


def _check_deps() -> None:
    print("Dependency check:")
    print(f"  Python    : {sys.version.split()[0]}")
    print(f"  pymongo   : {'✓ installed' if _has_pymongo() else '✗ not installed  (pip install pymongo)'}")
    print(f"  .NET SDK  : {'✓ available (>= 8)' if _has_dotnet() else '✗ not available'}")
    print(f"  replicator.py  : {'✓ found' if REPLICATOR_PY.exists() else '✗ missing'}")
    print(f"  .csproj        : {'✓ found' if CSPROJ.exists() else '✗ missing'}")


# ---------------------------------------------------------------------------
# Input helpers
# ---------------------------------------------------------------------------

def _prompt(label: str, default: str = "") -> str:
    suffix = f" [{default}]" if default else ""
    answer = input(f"{label}{suffix}: ").strip()
    return answer if answer else default


def _parse_date(value: str) -> datetime:
    """Parse yyyy-MM-dd or yyyy-MM-dd HH:mm:ss → UTC midnight datetime."""
    for fmt in ("%Y-%m-%d %H:%M:%S", "%Y-%m-%d"):
        try:
            dt = datetime.strptime(value.strip(), fmt)
            return dt.replace(hour=0, minute=0, second=0, microsecond=0,
                              tzinfo=timezone.utc)
        except ValueError:
            continue
    raise ValueError(f"Cannot parse date '{value}'. Use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss.")


def _get_start_date(cli_value: str | None) -> datetime:
    if cli_value:
        return _parse_date(cli_value)
    while True:
        raw = _prompt("Start date (yyyy-MM-dd or yyyy-MM-dd HH:mm:ss)")
        try:
            return _parse_date(raw)
        except ValueError as e:
            print(f"  {e}")


def _get_hours(cli_value: int | None) -> int:
    if cli_value and cli_value > 0:
        return cli_value
    while True:
        raw = _prompt("Duration in hours (e.g. 24 for full day)", "24")
        try:
            h = int(raw)
            if h > 0:
                return h
        except ValueError:
            pass
        print("  Enter a positive integer.")


# ---------------------------------------------------------------------------
# Python backend
# ---------------------------------------------------------------------------

def _run_python(args: argparse.Namespace) -> int:
    if not _has_pymongo():
        print("ERROR: pymongo is not installed.")
        print("       Run:  pip install pymongo")
        return 1

    # Import replicator from the same directory
    sys.path.insert(0, str(SCRIPT_DIR))
    from replicator import FastReplicator  # type: ignore[import]

    mongo_uri  = args.uri        or _prompt("MongoDB URI",      DEFAULT_MONGO_URI)
    db_name    = args.db         or _prompt("Database name",    DEFAULT_DB_NAME)
    collection = args.collection or _prompt("Collection name",  DEFAULT_COLLECTION)

    print()
    print(f"Connecting to  {mongo_uri}  /  {db_name}  /  {collection} …")

    r = FastReplicator(mongo_uri, db_name, collection)
    try:
        r.connect()
    except Exception as e:
        print(f"ERROR: Could not connect: {e}")
        return 1

    print()
    print("Checking database …")
    try:
        r.print_status()
    except Exception as e:
        print(f"ERROR: {e}")
        r.close()
        return 1

    print()
    start  = _get_start_date(args.start)
    hours  = _get_hours(args.hours)
    end    = start.replace(hour=0, minute=0, second=0, microsecond=0)
    from datetime import timedelta
    end    = start + timedelta(hours=hours)

    print()
    print("Replay configuration:")
    print(f"  Start (UTC) : {start.strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"  End   (UTC) : {end.strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"  Duration    : {hours} hour(s)")
    print()
    print("Starting replay …")
    print()

    try:
        r.replay(start, end, verbose=args.verbose)
    except KeyboardInterrupt:
        print("\nReplay interrupted by user.")
    finally:
        r.close()

    print()
    print("Done.")
    return 0


# ---------------------------------------------------------------------------
# .NET backend
# ---------------------------------------------------------------------------

def _run_dotnet(args: argparse.Namespace) -> int:
    if not _has_dotnet():
        print("ERROR: .NET 8 SDK is not installed.")
        print("       Download from: https://dotnet.microsoft.com/download/dotnet/8.0")
        return 1

    if not CSPROJ.exists():
        print(f"ERROR: .csproj not found: {CSPROJ}")
        return 1

    print(f"Building {CSPROJ.name} …")
    build = subprocess.run(
        ["dotnet", "build", str(CSPROJ), "--configuration", "Release", "--nologo"],
        cwd=str(SCRIPT_DIR),
    )
    if build.returncode != 0:
        print("Build failed.")
        return build.returncode

    print()
    cmd = ["dotnet", "run", "--project", str(CSPROJ), "--configuration", "Release", "--"]
    if args.start:
        cmd += ["--start", args.start]
    if args.hours:
        cmd += ["--hours", str(args.hours)]

    run = subprocess.run(cmd, cwd=str(SCRIPT_DIR))
    return run.returncode


# ---------------------------------------------------------------------------
# Backend auto-selection
# ---------------------------------------------------------------------------

def _select_backend(args: argparse.Namespace) -> str:
    if args.python:
        return "python"
    if args.dotnet:
        return "dotnet"

    # Auto-detect: prefer Python if pymongo is installed
    if _has_pymongo():
        return "python"
    if _has_dotnet():
        return "dotnet"

    print("ERROR: Neither pymongo nor the .NET 8 SDK is available.")
    print("  Install pymongo  :  pip install pymongo")
    print("  Install .NET SDK :  https://dotnet.microsoft.com/download/dotnet/8.0")
    sys.exit(1)


# ---------------------------------------------------------------------------
# CLI argument parsing
# ---------------------------------------------------------------------------

def _build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(
        prog="run_replicator.py",
        description="FAST Market Data Replicator – standalone launcher",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python run_replicator.py                            # fully interactive
  python run_replicator.py --start 2024-03-13         # supply date, prompt rest
  python run_replicator.py --start 2024-03-13 --hours 8
  python run_replicator.py --python                   # force Python backend
  python run_replicator.py --dotnet                   # force .NET  backend
  python run_replicator.py --check-deps               # list available dependencies
  python run_replicator.py --uri mongodb://host:27017 --db MyDb --start 2024-03-13 --hours 24
        """,
    )

    # Backend selection
    grp = p.add_mutually_exclusive_group()
    grp.add_argument("--python", action="store_true",
                     help="Use the Python backend (requires pymongo)")
    grp.add_argument("--dotnet", action="store_true",
                     help="Use the .NET backend (requires .NET 8 SDK)")

    # Replay parameters
    p.add_argument("--start", metavar="DATE",
                   help="Start date: yyyy-MM-dd  or  yyyy-MM-dd HH:mm:ss")
    p.add_argument("--hours", type=int, metavar="N",
                   help="Replay duration in hours (default: prompted)")

    # MongoDB parameters (Python backend only)
    p.add_argument("--uri",        default="",  metavar="URI",
                   help=f"MongoDB connection URI (default: {DEFAULT_MONGO_URI})")
    p.add_argument("--db",         default="",  metavar="NAME",
                   help=f"Database name (default: {DEFAULT_DB_NAME})")
    p.add_argument("--collection", default="",  metavar="NAME",
                   help=f"Collection name (default: {DEFAULT_COLLECTION})")

    # Misc
    p.add_argument("--verbose", action="store_true",
                   help="Print per-message details for MDIncrementalRefresh")
    p.add_argument("--check-deps", action="store_true",
                   help="Report available dependencies and exit")

    return p


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

def main() -> int:
    parser = _build_parser()
    args   = parser.parse_args()

    # Always change to repo root so relative paths resolve correctly
    os.chdir(str(REPO_ROOT))

    print("=" * 60)
    print("  FAST Market Data Replicator")
    print("=" * 60)
    print()

    if args.check_deps:
        _check_deps()
        return 0

    backend = _select_backend(args)
    print(f"Backend: {backend.upper()}")
    print()

    if backend == "python":
        return _run_python(args)
    else:
        return _run_dotnet(args)


if __name__ == "__main__":
    sys.exit(main())
