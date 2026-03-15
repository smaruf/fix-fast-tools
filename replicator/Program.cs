using System.Globalization;
using System.Text.Json;
using FastTools.Core.Models;
using FastTools.Core.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace EcoSoftBD.Oms.Fast.ReplyFASTMarketData
{
    // ---------------------------------------------------------------------------
    // Local model mapping to the FastIncomingMessages MongoDB collection.
    // Fields match the schema used when the FAST client stores incoming packets.
    // ---------------------------------------------------------------------------
    public class FastIncomingMessage
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("Channel")]
        public string Channel { get; set; } = string.Empty;

        [BsonElement("PacketNum")]
        public long PacketNum { get; set; }

        [BsonElement("MsgName")]
        public string MsgName { get; set; } = string.Empty;

        [BsonElement("MsgText")]
        public string? MsgText { get; set; }

        [BsonElement("SendingDateTimeUtc")]
        public DateTime SendingDateTimeUtc { get; set; }

        [BsonElement("RawMsg")]
        public byte[]? RawMsg { get; set; }
    }

    // ---------------------------------------------------------------------------
    // Replay result produced for each message.
    // ---------------------------------------------------------------------------
    public class ReplayResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DecodedMessage? BinaryDecoded { get; set; }
        public Dictionary<string, JsonElement>? ParsedFields { get; set; }
    }

    // ---------------------------------------------------------------------------
    // Main program
    // ---------------------------------------------------------------------------
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("=== CSE FAST Market Data Replay Tool ===");
            Console.WriteLine();

            // Load configuration from appsettings.json (optional)
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            string mongoUri   = config["MongoDb:ServerFast"] ?? "mongodb://localhost:27017";
            string dbName     = config["MongoDb:FastDb"]     ?? "OmsTradingApi_CSE_FAST_DB";
            string collection = config["MongoDb:Collection"] ?? "FastIncomingMessages";

            // Optional: load FAST template XML for richer template name resolution
            var decoder      = new FastMessageDecoder();
            string templateXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FAST_TEMPLATE.xml");
            if (!File.Exists(templateXml))
                templateXml = Path.Combine("..", "Tools", "FAST_TEMPLATE.xml");
            if (File.Exists(templateXml))
                decoder.LoadTemplateMap(templateXml);

            // Parse optional CLI arguments: --start yyyy-MM-dd --hours N
            DateTime? cliStart = null;
            int?      cliHours = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--start" &&
                    DateTime.TryParseExact(args[i + 1], new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" },
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
                    cliStart = DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);

                if (args[i] == "--hours" && int.TryParse(args[i + 1], out int h) && h > 0)
                    cliHours = h;
            }

            // Connect to MongoDB
            IMongoClient mongoClient;
            IMongoDatabase db;
            IMongoCollection<FastIncomingMessage> col;
            try
            {
                var clientSettings = MongoClientSettings.FromConnectionString(mongoUri);
                clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
                mongoClient = new MongoClient(clientSettings);
                db  = mongoClient.GetDatabase(dbName);
                col = db.GetCollection<FastIncomingMessage>(collection);
                // Probe connectivity
                await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Cannot connect to MongoDB at {mongoUri}");
                Console.WriteLine($"       {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("Please update MongoDb:ServerFast and MongoDb:FastDb in appsettings.json.");
                return 1;
            }

            // Show DB statistics
            await ShowDatabaseStatusAsync(col);
            Console.WriteLine();

            // Determine replay window
            DateTime startDateTime = cliStart ?? GetStartDateTime();
            int durationHours      = cliHours  ?? GetDurationHours();
            DateTime endDateTime   = startDateTime.AddHours(durationHours);

            Console.WriteLine();
            Console.WriteLine("Replay Configuration:");
            Console.WriteLine($"  Start (UTC) : {startDateTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  End   (UTC) : {endDateTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"  Duration    : {durationHours} hour(s)");
            Console.WriteLine();

            // Run replay
            Console.WriteLine("Starting replay...");
            Console.WriteLine();

            try
            {
                await ReplayMessagesAsync(col, decoder, startDateTime, endDateTime);
                Console.WriteLine();
                Console.WriteLine("Replay completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return 1;
            }

            return 0;
        }

        // ------------------------------------------------------------------
        // Database status
        // ------------------------------------------------------------------
        static async Task ShowDatabaseStatusAsync(IMongoCollection<FastIncomingMessage> col)
        {
            Console.WriteLine("Checking database...");
            try
            {
                long total = await col.CountDocumentsAsync(FilterDefinition<FastIncomingMessage>.Empty);
                Console.WriteLine($"  Total messages : {total:N0}");

                if (total == 0)
                {
                    Console.WriteLine("  WARNING: Collection is empty – no messages to replay.");
                    return;
                }

                var earliest = await col.Find(FilterDefinition<FastIncomingMessage>.Empty)
                    .SortBy(x => x.SendingDateTimeUtc).Limit(1).FirstOrDefaultAsync();
                var latest   = await col.Find(FilterDefinition<FastIncomingMessage>.Empty)
                    .SortByDescending(x => x.SendingDateTimeUtc).Limit(1).FirstOrDefaultAsync();

                if (earliest != null && latest != null)
                {
                    Console.WriteLine($"  Date range     : {earliest.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss} UTC  →  {latest.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss} UTC");
                }

                // Distinct message types with counts
                var uniqueTypes = await col
                    .DistinctAsync(new StringFieldDefinition<FastIncomingMessage, string>("MsgName"),
                                   FilterDefinition<FastIncomingMessage>.Empty);
                var typeList = (await uniqueTypes.ToListAsync()).OrderBy(t => t).ToList();
                Console.WriteLine($"  Message types  : {typeList.Count}");
                foreach (var msgType in typeList)
                {
                    long cnt = await col.CountDocumentsAsync(
                        Builders<FastIncomingMessage>.Filter.Eq(m => m.MsgName, msgType));
                    Console.WriteLine($"    {msgType,-40}  {cnt,8:N0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR querying database: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Interactive input helpers
        // ------------------------------------------------------------------
        static DateTime GetStartDateTime()
        {
            while (true)
            {
                Console.Write("Enter start date (yyyy-MM-dd) or datetime (yyyy-MM-dd HH:mm:ss): ");
                string? input = Console.ReadLine()?.Trim();

                if (DateTime.TryParseExact(input,
                        new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" },
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
                    return DateTime.SpecifyKind(dt.Date, DateTimeKind.Utc);

                if (DateTime.TryParse(input, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var dt2))
                    return DateTime.SpecifyKind(dt2.Date, DateTimeKind.Utc);

                Console.WriteLine("  Invalid format. Examples: 2024-03-13  or  2024-03-13 09:00:00");
            }
        }

        static int GetDurationHours()
        {
            while (true)
            {
                Console.Write("Enter duration in hours (e.g. 24 for a full day): ");
                string? input = Console.ReadLine()?.Trim();
                if (int.TryParse(input, out int h) && h > 0) return h;
                Console.WriteLine("  Invalid input – enter a positive integer.");
            }
        }

        // ------------------------------------------------------------------
        // Main replay loop
        // ------------------------------------------------------------------
        static async Task ReplayMessagesAsync(
            IMongoCollection<FastIncomingMessage> col,
            FastMessageDecoder decoder,
            DateTime startDateTime,
            DateTime endDateTime)
        {
            var filter = Builders<FastIncomingMessage>.Filter.Gte(x => x.SendingDateTimeUtc, startDateTime)
                       & Builders<FastIncomingMessage>.Filter.Lt(x  => x.SendingDateTimeUtc, endDateTime);

            var messages = await col.Find(filter)
                .SortBy(x => x.SendingDateTimeUtc)
                .ToListAsync();

            int total     = messages.Count;
            int processed = 0;
            int errors    = 0;
            var errorSummary  = new Dictionary<string, int>();
            var errorExamples = new Dictionary<string, string>();

            Console.WriteLine($"Found {total:N0} messages to replay");
            Console.WriteLine();

            foreach (var msg in messages)
            {
                var result = ProcessMessage(msg, decoder);

                if (result.Success)
                {
                    processed++;
                    if (processed % 100 == 0)
                        Console.WriteLine($"  Processed {processed:N0} messages...");
                }
                else
                {
                    errors++;
                    string key = $"{msg.MsgName}: {result.Error ?? "Unknown"}";
                    errorSummary.TryGetValue(key, out int prev);
                    errorSummary[key] = prev + 1;
                    if (!errorExamples.ContainsKey(key))
                        errorExamples[key] = $"{msg.SendingDateTimeUtc:yyyy-MM-dd HH:mm:ss.fff} ch={msg.Channel}";
                }
            }

            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine($"  Total messages       : {total:N0}");
            Console.WriteLine($"  Successfully decoded : {processed:N0}");
            Console.WriteLine($"  Errors               : {errors:N0}");

            if (errors > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Error Breakdown:");
                foreach (var kv in errorSummary.OrderByDescending(x => x.Value))
                {
                    Console.WriteLine($"  {kv.Value,6}x  {kv.Key}");
                    if (errorExamples.TryGetValue(kv.Key, out var ex))
                        Console.WriteLine($"          first: {ex}");
                }
            }
        }

        // ------------------------------------------------------------------
        // Per-message processing: binary decode + JSON field extraction
        // ------------------------------------------------------------------
        static ReplayResult ProcessMessage(FastIncomingMessage msg, FastMessageDecoder decoder)
        {
            // Skip known non-JSON binary stubs
            if (string.IsNullOrEmpty(msg.MsgText) && (msg.RawMsg == null || msg.RawMsg.Length == 0))
                return new ReplayResult { Success = false, Error = "Empty message (no MsgText, no RawMsg)" };

            if (msg.MsgText?.StartsWith("RAW_HEX:", StringComparison.OrdinalIgnoreCase) == true)
                return new ReplayResult { Success = false, Error = "RAW_HEX binary stub – not deserializable" };

            if (msg.MsgText?.StartsWith("RAW_EMPTY", StringComparison.OrdinalIgnoreCase) == true)
                return new ReplayResult { Success = false, Error = "RAW_EMPTY packet" };

            var result = new ReplayResult { Success = true };

            // 1. Decode raw binary with FastMessageDecoder when available
            if (msg.RawMsg != null && msg.RawMsg.Length > 0)
            {
                try { result.BinaryDecoded = decoder.DecodeBinary(msg.RawMsg); }
                catch { /* non-fatal: fall through to JSON path */ }
            }

            // 2. Parse MsgText JSON fields for structured logging
            if (!string.IsNullOrWhiteSpace(msg.MsgText))
            {
                try
                {
                    using var doc = JsonDocument.Parse(msg.MsgText);
                    result.ParsedFields = doc.RootElement.EnumerateObject()
                        .ToDictionary(p => p.Name, p => p.Value.Clone());
                }
                catch { /* non-fatal */ }
            }

            // Must have decoded something to count as success
            if (result.BinaryDecoded == null && result.ParsedFields == null)
                return new ReplayResult { Success = false, Error = $"Could not decode {msg.MsgName}" };

            return result;
        }
    }
}
