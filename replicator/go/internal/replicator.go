// Package replicator provides the core logic for replaying historical CSE FAST
// market data messages stored in a MongoDB collection.
//
// It includes a FAST stop-bit integer decoder, a binary payload decoder, and a
// Replicator type that queries MongoDB and processes each message in
// chronological order.
//
// Typical use:
//
//	r, err := replicator.New("mongodb://localhost:27017", "OmsTradingApi_CSE_FAST_DB", "FastIncomingMessages")
//	if err != nil {
//	    log.Fatal(err)
//	}
//	defer r.Close()
//	stats, err := r.Replay(ctx, start, end, replicator.Options{ProgressInterval: 100})
package internal

import (
	"context"
	"encoding/base64"
	"encoding/hex"
	"encoding/json"
	"fmt"
	"sort"
	"strings"
	"time"
	"unicode"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

// ---------------------------------------------------------------------------
// FAST template ID → name map (CSE FAST template set)
// ---------------------------------------------------------------------------

// TemplateNames maps FAST template IDs to human-readable message names.
var TemplateNames = map[int]string{
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

// ---------------------------------------------------------------------------
// FAST binary decoder
// ---------------------------------------------------------------------------

// readStopBitUint reads a FAST stop-bit-encoded unsigned integer from data
// starting at offset. Returns (value, bytesRead). Returns (0, 0) on failure.
func readStopBitUint(data []byte, offset int) (int, int) {
	value := 0
	read := 0
	for i := offset; i < len(data) && read < 5; i++ {
		b := data[i]
		value = (value << 7) | int(b&0x7F)
		read++
		if b&0x80 != 0 { // stop bit set
			return value, read
		}
	}
	return 0, 0
}

// DecodedBinary holds the result of decoding a raw FAST binary payload.
type DecodedBinary struct {
	Hex          string
	TemplateID   int
	TemplateName string
	Strings      []string
	Integers     []int
}

// DecodeBinary decodes a raw FAST binary payload.
func DecodeBinary(data []byte, extraNames map[int]string) DecodedBinary {
	result := DecodedBinary{
		Hex:      strings.ToUpper(hex.EncodeToString(data)),
		Strings:  []string{},
		Integers: []int{},
	}

	if len(data) == 0 {
		return result
	}

	// Merged template name map
	tmap := make(map[int]string, len(TemplateNames)+len(extraNames))
	for k, v := range TemplateNames {
		tmap[k] = v
	}
	for k, v := range extraNames {
		tmap[k] = v
	}

	// First stop-bit uint is the FAST template ID.
	// read == 0 means the buffer is too short or the encoding is invalid
	// (not the same as a template ID whose value happens to be zero).
	tid, read := readStopBitUint(data, 0)
	if read > 0 {
		result.TemplateID = tid
		if name, ok := tmap[tid]; ok {
			result.TemplateName = name
		} else {
			result.TemplateName = fmt.Sprintf("Unknown(%d)", tid)
		}
	}
	// If read == 0 the payload is too short to contain a valid template header;
	// TemplateID stays at its zero value and TemplateName stays empty.

	// Extract printable ASCII runs (≥ 3 chars)
	var run []rune
	flush := func() {
		if len(run) >= 3 {
			result.Strings = append(result.Strings, string(run))
		}
		run = run[:0]
	}
	for _, b := range data {
		r := rune(b)
		if r >= 32 && r <= 126 && unicode.IsPrint(r) {
			run = append(run, r)
		} else {
			flush()
		}
	}
	flush()

	// Extract first ≤ 10 stop-bit integers
	pos := 0
	for i := 0; i < 10 && pos < len(data); i++ {
		v, rd := readStopBitUint(data, pos)
		if rd == 0 {
			break
		}
		result.Integers = append(result.Integers, v)
		pos += rd
	}

	return result
}

// ---------------------------------------------------------------------------
// MongoDB document model
// ---------------------------------------------------------------------------

// FastIncomingMessage mirrors the MongoDB FastIncomingMessages document schema.
type FastIncomingMessage struct {
	ID                 primitive.ObjectID `bson:"_id,omitempty"`
	Channel            string             `bson:"Channel"`
	PacketNum          int64              `bson:"PacketNum"`
	MsgName            string             `bson:"MsgName"`
	MsgText            string             `bson:"MsgText"`
	SendingDateTimeUtc time.Time          `bson:"SendingDateTimeUtc"`
	RawMsg             primitive.Binary   `bson:"RawMsg,omitempty"`
}

// ---------------------------------------------------------------------------
// Single-message processing
// ---------------------------------------------------------------------------

// ProcessResult holds the outcome of processing a single message.
type ProcessResult struct {
	Success bool
	Error   string
	Binary  *DecodedBinary
	Fields  map[string]interface{}
}

// ProcessMessage decodes one FastIncomingMessage.
func ProcessMessage(msg FastIncomingMessage, extraNames map[int]string) ProcessResult {
	// Guard: nothing to decode
	if msg.MsgText == "" && len(msg.RawMsg.Data) == 0 {
		return ProcessResult{
			Error: fmt.Sprintf("%s: empty message (no MsgText, no RawMsg)", msg.MsgName),
		}
	}

	if strings.HasPrefix(msg.MsgText, "RAW_HEX:") {
		return ProcessResult{
			Error: fmt.Sprintf("%s: RAW_HEX binary stub – not deserializable", msg.MsgName),
		}
	}
	if strings.HasPrefix(msg.MsgText, "RAW_EMPTY") {
		return ProcessResult{
			Error: fmt.Sprintf("%s: RAW_EMPTY packet", msg.MsgName),
		}
	}

	result := ProcessResult{Success: true}

	// 1. Decode binary payload when present
	rawBytes := msg.RawMsg.Data
	if len(rawBytes) == 0 && msg.MsgText != "" {
		// Try Base64-decoding MsgText in case it holds encoded binary
		if decoded, err := base64.StdEncoding.DecodeString(msg.MsgText); err == nil {
			rawBytes = decoded
		}
	}
	if len(rawBytes) > 0 {
		b := DecodeBinary(rawBytes, extraNames)
		result.Binary = &b
	}

	// 2. Parse structured JSON fields from MsgText
	if msg.MsgText != "" {
		var fields map[string]interface{}
		if err := json.Unmarshal([]byte(msg.MsgText), &fields); err == nil {
			result.Fields = fields
		}
	}

	// At least one decode path must have produced output
	if result.Binary == nil && result.Fields == nil {
		return ProcessResult{
			Error: fmt.Sprintf("could not decode %s", msg.MsgName),
		}
	}

	return result
}

// ---------------------------------------------------------------------------
// Replicator
// ---------------------------------------------------------------------------

// Options controls replay behaviour.
type Options struct {
	// ProgressInterval prints a status line every N successfully processed messages.
	// Defaults to 100.
	ProgressInterval int
	// Verbose prints per-message details for MDIncrementalRefresh messages.
	Verbose bool
	// ExtraTemplateNames extends TemplateNames with additional ID→name mappings.
	ExtraTemplateNames map[int]string
}

// Stats summarises a completed replay.
type Stats struct {
	Total        int
	Processed    int
	Errors       int
	ErrorSummary map[string]int
}

// Replicator connects to MongoDB and replays FastIncomingMessage documents.
type Replicator struct {
	mongoURI       string
	dbName         string
	collectionName string
	client         *mongo.Client
	collection     *mongo.Collection
}

// New creates a new Replicator and opens the MongoDB connection.
// Call Close() when done.
func New(mongoURI, dbName, collectionName string) (*Replicator, error) {
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	clientOpts := options.Client().ApplyURI(mongoURI).
		SetServerSelectionTimeout(5 * time.Second)

	client, err := mongo.Connect(ctx, clientOpts)
	if err != nil {
		return nil, fmt.Errorf("connect: %w", err)
	}
	// Probe connectivity
	if err := client.Ping(ctx, nil); err != nil {
		_ = client.Disconnect(context.Background())
		return nil, fmt.Errorf("ping: %w", err)
	}

	col := client.Database(dbName).Collection(collectionName)
	return &Replicator{
		mongoURI:       mongoURI,
		dbName:         dbName,
		collectionName: collectionName,
		client:         client,
		collection:     col,
	}, nil
}

// Close disconnects from MongoDB.
func (r *Replicator) Close() {
	if r.client != nil {
		_ = r.client.Disconnect(context.Background())
	}
}

// StatusInfo holds collection statistics.
type StatusInfo struct {
	Total        int64
	Earliest     time.Time
	Latest       time.Time
	MessageTypes []MessageTypeCount
}

// MessageTypeCount holds a (name, count) pair.
type MessageTypeCount struct {
	Name  string
	Count int64
}

// Status returns statistics about the collection.
func (r *Replicator) Status(ctx context.Context) (StatusInfo, error) {
	var info StatusInfo

	total, err := r.collection.CountDocuments(ctx, bson.D{})
	if err != nil {
		return info, err
	}
	info.Total = total

	if total == 0 {
		return info, nil
	}

	// Earliest message
	var earliest FastIncomingMessage
	opts := options.FindOne().SetSort(bson.D{{Key: "SendingDateTimeUtc", Value: 1}})
	if err := r.collection.FindOne(ctx, bson.D{}, opts).Decode(&earliest); err == nil {
		info.Earliest = earliest.SendingDateTimeUtc
	}

	// Latest message
	var latest FastIncomingMessage
	opts = options.FindOne().SetSort(bson.D{{Key: "SendingDateTimeUtc", Value: -1}})
	if err := r.collection.FindOne(ctx, bson.D{}, opts).Decode(&latest); err == nil {
		info.Latest = latest.SendingDateTimeUtc
	}

	// Per-type counts via aggregation
	pipeline := mongo.Pipeline{
		{{Key: "$group", Value: bson.D{
			{Key: "_id", Value: "$MsgName"},
			{Key: "count", Value: bson.D{{Key: "$sum", Value: 1}}},
		}}},
		{{Key: "$sort", Value: bson.D{{Key: "_id", Value: 1}}}},
	}
	cursor, err := r.collection.Aggregate(ctx, pipeline)
	if err != nil {
		return info, err
	}
	defer cursor.Close(ctx)

	for cursor.Next(ctx) {
		var row struct {
			Name  string `bson:"_id"`
			Count int64  `bson:"count"`
		}
		if err := cursor.Decode(&row); err == nil {
			info.MessageTypes = append(info.MessageTypes, MessageTypeCount{
				Name:  row.Name,
				Count: row.Count,
			})
		}
	}
	return info, cursor.Err()
}

// PrintStatus prints a human-readable status to stdout.
func (r *Replicator) PrintStatus(ctx context.Context) error {
	info, err := r.Status(ctx)
	if err != nil {
		return err
	}
	fmt.Printf("  Total messages : %d\n", info.Total)
	if info.Total == 0 {
		fmt.Println("  WARNING: Collection is empty – no messages to replay.")
		return nil
	}
	if !info.Earliest.IsZero() && !info.Latest.IsZero() {
		fmt.Printf("  Date range     : %s UTC  →  %s UTC\n",
			info.Earliest.UTC().Format("2006-01-02 15:04:05"),
			info.Latest.UTC().Format("2006-01-02 15:04:05"))
	}
	fmt.Printf("  Message types  : %d\n", len(info.MessageTypes))
	for _, mt := range info.MessageTypes {
		fmt.Printf("    %-40s  %8d\n", mt.Name, mt.Count)
	}
	return nil
}

// GetMessages returns all messages in the half-open interval [start, end).
func (r *Replicator) GetMessages(ctx context.Context, start, end time.Time) ([]FastIncomingMessage, error) {
	filter := bson.D{
		{Key: "SendingDateTimeUtc", Value: bson.D{
			{Key: "$gte", Value: start},
			{Key: "$lt", Value: end},
		}},
	}
	opts := options.Find().SetSort(bson.D{{Key: "SendingDateTimeUtc", Value: 1}})
	cursor, err := r.collection.Find(ctx, filter, opts)
	if err != nil {
		return nil, err
	}
	defer cursor.Close(ctx)

	var msgs []FastIncomingMessage
	if err := cursor.All(ctx, &msgs); err != nil {
		return nil, err
	}
	return msgs, nil
}

// Replay processes all messages in [start, end) and returns summary statistics.
func (r *Replicator) Replay(ctx context.Context, start, end time.Time, opts Options) (Stats, error) {
	if opts.ProgressInterval <= 0 {
		opts.ProgressInterval = 100
	}

	messages, err := r.GetMessages(ctx, start, end)
	if err != nil {
		return Stats{}, err
	}

	total := len(messages)
	processed := 0
	errors := 0
	errorSummary := make(map[string]int)
	errorExamples := make(map[string]string)

	fmt.Printf("Found %d messages to replay\n\n", total)

	for _, msg := range messages {
		result := ProcessMessage(msg, opts.ExtraTemplateNames)
		tsStr := msg.SendingDateTimeUtc.UTC().Format("2006-01-02 15:04:05.000")

		if result.Success {
			processed++

			if opts.Verbose && msg.MsgName == "MDIncrementalRefresh" {
				entries := 0
				if result.Fields != nil {
					if incRef, ok := result.Fields["IncRefMDEntries"].(map[string]interface{}); ok {
						if items, ok := incRef["Items"].([]interface{}); ok {
							entries = len(items)
						}
					}
				}
				fmt.Printf("  ✓ MDIncrementalRefresh  %s  ch=%s  entries=%d\n",
					tsStr, msg.Channel, entries)
			}

			if processed%opts.ProgressInterval == 0 {
				fmt.Printf("  Processed %d messages...\n", processed)
			}
		} else {
			errors++
			key := result.Error
			if key == "" {
				key = "Unknown error"
			}
			errorSummary[key]++
			if _, seen := errorExamples[key]; !seen {
				errorExamples[key] = fmt.Sprintf("%s  ch=%s", tsStr, msg.Channel)
			}
		}
	}

	fmt.Println()
	fmt.Println("Summary:")
	fmt.Printf("  Total messages       : %d\n", total)
	fmt.Printf("  Successfully decoded : %d\n", processed)
	fmt.Printf("  Errors               : %d\n", errors)

	if len(errorSummary) > 0 {
		fmt.Println()
		fmt.Println("Error Breakdown:")

		// Sort errors by count descending
		type kv struct {
			Key   string
			Count int
		}
		var sorted []kv
		for k, v := range errorSummary {
			sorted = append(sorted, kv{k, v})
		}
		sort.Slice(sorted, func(i, j int) bool { return sorted[i].Count > sorted[j].Count })

		for _, item := range sorted {
			fmt.Printf("  %6dx  %s\n", item.Count, item.Key)
			if ex, ok := errorExamples[item.Key]; ok {
				fmt.Printf("          first: %s\n", ex)
			}
		}
	}

	return Stats{
		Total:        total,
		Processed:    processed,
		Errors:       errors,
		ErrorSummary: errorSummary,
	}, nil
}
