// main.go — CLI entry point for the CSE FAST Market Data Replay Tool (Go).
//
// Usage:
//
//	go run . [flags]
//
// Flags:
//
//	-uri      MongoDB connection URI (default: mongodb://localhost:27017)
//	-db       Database name           (default: OmsTradingApi_CSE_FAST_DB)
//	-col      Collection name         (default: FastIncomingMessages)
//	-start    Start date yyyy-MM-dd or yyyy-MM-dd HH:mm:ss (prompted if omitted)
//	-hours    Replay duration in hours (prompted if omitted)
//	-verbose  Print per-message details for MDIncrementalRefresh
//
// When -start or -hours are not provided the tool prompts interactively.
package main

import (
	"bufio"
	"context"
	"flag"
	"fmt"
	"os"
	"strconv"
	"strings"
	"time"

	repl "github.com/smaruf/fix-fast-tools/replicator/internal"
)

func main() {
	fmt.Println("=== CSE FAST Market Data Replay Tool (Go) ===")
	fmt.Println()

	// --- CLI flags ---
	uri     := flag.String("uri",     "mongodb://localhost:27017",  "MongoDB connection URI")
	dbName  := flag.String("db",      "OmsTradingApi_CSE_FAST_DB", "MongoDB database name")
	col     := flag.String("col",     "FastIncomingMessages",       "MongoDB collection name")
	startS  := flag.String("start",   "",                           "Start date: yyyy-MM-dd or yyyy-MM-dd HH:mm:ss (UTC)")
	hoursN  := flag.Int("hours",      0,                            "Replay duration in hours (> 0)")
	verbose := flag.Bool("verbose",   false,                        "Verbose output for MDIncrementalRefresh messages")
	flag.Parse()

	// --- Connect ---
	fmt.Printf("Connecting to %s / %s / %s …\n", *uri, *dbName, *col)
	r, err := repl.New(*uri, *dbName, *col)
	if err != nil {
		fmt.Fprintf(os.Stderr, "ERROR: Cannot connect to MongoDB: %v\n", err)
		fmt.Fprintln(os.Stderr, "       Use -uri, -db, and -col flags to configure the connection.")
		fmt.Fprintln(os.Stderr, "       Example: go run . -uri mongodb://host:27017 -db MyDb")
		os.Exit(1)
	}
	defer r.Close()

	// --- DB status ---
	fmt.Println()
	fmt.Println("Checking database …")
	ctx := context.Background()
	if err := r.PrintStatus(ctx); err != nil {
		fmt.Fprintf(os.Stderr, "ERROR querying database: %v\n", err)
		os.Exit(1)
	}
	fmt.Println()

	// --- Replay window ---
	start := mustParseStartDate(*startS)
	hours := mustGetHours(*hoursN)
	end   := start.Add(time.Duration(hours) * time.Hour)

	fmt.Println()
	fmt.Println("Replay configuration:")
	fmt.Printf("  Start (UTC) : %s\n", start.UTC().Format("2006-01-02 15:04:05"))
	fmt.Printf("  End   (UTC) : %s\n", end.UTC().Format("2006-01-02 15:04:05"))
	fmt.Printf("  Duration    : %d hour(s)\n", hours)
	fmt.Println()
	fmt.Println("Starting replay …")
	fmt.Println()

	// --- Replay ---
	_, err = r.Replay(ctx, start, end, repl.Options{
		ProgressInterval: 100,
		Verbose:          *verbose,
	})
	if err != nil {
		fmt.Fprintf(os.Stderr, "ERROR during replay: %v\n", err)
		os.Exit(1)
	}

	fmt.Println()
	fmt.Println("Replay completed successfully!")
}

// ---------------------------------------------------------------------------
// Input helpers
// ---------------------------------------------------------------------------

var stdin = bufio.NewReader(os.Stdin)

func readLine(prompt string) string {
	fmt.Print(prompt)
	line, _ := stdin.ReadString('\n')
	return strings.TrimSpace(line)
}

func parseStartDate(s string) (time.Time, error) {
	s = strings.TrimSpace(s)
	for _, layout := range []string{"2006-01-02 15:04:05", "2006-01-02"} {
		if t, err := time.ParseInLocation(layout, s, time.UTC); err == nil {
			// Always replay from midnight of that date
			return time.Date(t.Year(), t.Month(), t.Day(), 0, 0, 0, 0, time.UTC), nil
		}
	}
	return time.Time{}, fmt.Errorf("cannot parse %q – use yyyy-MM-dd or yyyy-MM-dd HH:mm:ss", s)
}

func mustParseStartDate(cliValue string) time.Time {
	if cliValue != "" {
		t, err := parseStartDate(cliValue)
		if err != nil {
			fmt.Fprintln(os.Stderr, "ERROR:", err)
			os.Exit(1)
		}
		return t
	}
	for {
		raw := readLine("Enter start date (yyyy-MM-dd or yyyy-MM-dd HH:mm:ss): ")
		t, err := parseStartDate(raw)
		if err == nil {
			return t
		}
		fmt.Println(" ", err)
	}
}

func mustGetHours(cliValue int) int {
	if cliValue > 0 {
		return cliValue
	}
	for {
		raw := readLine("Enter duration in hours (e.g. 24 for full day): ")
		h, err := strconv.Atoi(raw)
		if err == nil && h > 0 {
			return h
		}
		fmt.Println("  Enter a positive integer.")
	}
}
