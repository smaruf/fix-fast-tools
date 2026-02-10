using System.Text;
using System.Text.RegularExpressions;

namespace ChinPakTools.DSE
{
    public class SessionLogAnalyzer
    {

        public SessionLogStats AnalyzeLogFile(string logFilePath)
        {
            if (!File.Exists(logFilePath))
                throw new FileNotFoundException($"Log file not found: {logFilePath}");

            var stats = new SessionLogStats
            {
                FilePath = logFilePath,
                AnalysisTime = DateTime.Now
            };

            var lines = File.ReadAllLines(logFilePath);
            
            foreach (var line in lines)
            {
                stats.TotalLines++;
                AnalyzeLine(line, stats);
            }

            stats.Duration = stats.LastMessageTime - stats.FirstMessageTime;
            return stats;
        }

        private void AnalyzeLine(string line, SessionLogStats stats)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                stats.EmptyLines++;
                return;
            }

            // Extract timestamp if present
            var timestampMatch = Regex.Match(line, @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})");
            if (timestampMatch.Success)
            {
                if (DateTime.TryParse(timestampMatch.Groups[1].Value, out var timestamp))
                {
                    if (stats.FirstMessageTime == default)
                        stats.FirstMessageTime = timestamp;
                    stats.LastMessageTime = timestamp;
                }
            }

            // Detect FIX message
            if (line.Contains("8=FIX") || line.Contains("35="))
            {
                stats.FixMessages++;
                var msgType = ExtractMessageType(line);
                
                if (!string.IsNullOrEmpty(msgType))
                {
                    if (!stats.MessageTypeCount.ContainsKey(msgType))
                        stats.MessageTypeCount[msgType] = 0;
                    stats.MessageTypeCount[msgType]++;
                }

                // Direction detection
                if (line.Contains("OUTGOING") || line.Contains(">>>") || line.Contains("SENT"))
                    stats.OutgoingMessages++;
                else if (line.Contains("INCOMING") || line.Contains("<<<") || line.Contains("RECEIVED"))
                    stats.IncomingMessages++;
            }

            // Detect errors
            if (line.Contains("ERROR") || line.Contains("REJECT") || line.Contains("Exception"))
            {
                stats.ErrorCount++;
                stats.ErrorMessages.Add(line);
            }

            // Detect session events
            if (line.Contains("Logon") || line.Contains("LOGON"))
                stats.LogonCount++;
            else if (line.Contains("Logout") || line.Contains("LOGOUT"))
                stats.LogoutCount++;
            else if (line.Contains("Heartbeat") || line.Contains("HEARTBEAT"))
                stats.HeartbeatCount++;
        }

        private string? ExtractMessageType(string line)
        {
            // Try to find tag 35 (MsgType)
            var match = Regex.Match(line, @"35=([^\x01|\s]+)");
            if (match.Success)
                return match.Groups[1].Value;
            
            return null;
        }

        public void PrintStats(SessionLogStats stats)
        {
            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine("FIX SESSION LOG ANALYSIS");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine($"File: {Path.GetFileName(stats.FilePath)}");
            Console.WriteLine($"Analysis Time: {stats.AnalysisTime:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine(new string('-', 70));
            
            Console.WriteLine("\n📊 GENERAL STATISTICS");
            Console.WriteLine($"  Total Lines:        {stats.TotalLines:N0}");
            Console.WriteLine($"  Empty Lines:        {stats.EmptyLines:N0}");
            Console.WriteLine($"  FIX Messages:       {stats.FixMessages:N0}");
            
            if (stats.FirstMessageTime != default)
            {
                Console.WriteLine($"\n⏰ SESSION TIMELINE");
                Console.WriteLine($"  First Message:      {stats.FirstMessageTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Last Message:       {stats.LastMessageTime:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"  Duration:           {stats.Duration}");
            }
            
            Console.WriteLine($"\n📨 MESSAGE DIRECTION");
            Console.WriteLine($"  Outgoing:           {stats.OutgoingMessages:N0}");
            Console.WriteLine($"  Incoming:           {stats.IncomingMessages:N0}");
            
            Console.WriteLine($"\n🔄 SESSION EVENTS");
            Console.WriteLine($"  Logon:              {stats.LogonCount:N0}");
            Console.WriteLine($"  Logout:             {stats.LogoutCount:N0}");
            Console.WriteLine($"  Heartbeats:         {stats.HeartbeatCount:N0}");
            
            if (stats.MessageTypeCount.Count > 0)
            {
                Console.WriteLine($"\n📋 MESSAGE TYPES");
                foreach (var kvp in stats.MessageTypeCount.OrderByDescending(x => x.Value))
                {
                    var msgTypeName = GetMessageTypeName(kvp.Key);
                    Console.WriteLine($"  {kvp.Key} - {msgTypeName,-25} {kvp.Value,6:N0}");
                }
            }
            
            if (stats.ErrorCount > 0)
            {
                Console.WriteLine($"\n⚠️  ERRORS: {stats.ErrorCount:N0}");
                foreach (var error in stats.ErrorMessages.Take(5))
                {
                    var displayError = error.Length > 60 ? error.Substring(0, 60) + "..." : error;
                    Console.WriteLine($"  • {displayError}");
                }
                if (stats.ErrorMessages.Count > 5)
                    Console.WriteLine($"  ... and {stats.ErrorMessages.Count - 5} more errors");
            }
            
            Console.WriteLine("\n" + new string('=', 70));
        }

        private string GetMessageTypeName(string msgType)
        {
            return msgType switch
            {
                "0" => "Heartbeat",
                "1" => "Test Request",
                "2" => "Resend Request",
                "3" => "Reject",
                "4" => "Sequence Reset",
                "5" => "Logout",
                "8" => "Execution Report",
                "9" => "Order Cancel Reject",
                "A" => "Logon",
                "D" => "New Order Single",
                "F" => "Order Cancel/Replace",
                "G" => "Order Cancel Request",
                "H" => "Order Status Request",
                "j" => "Business Reject",
                _ => "Unknown"
            };
        }
    }

    public class SessionLogStats
    {
        public string FilePath { get; set; } = string.Empty;
        public DateTime AnalysisTime { get; set; }
        public int TotalLines { get; set; }
        public int EmptyLines { get; set; }
        public int FixMessages { get; set; }
        public int OutgoingMessages { get; set; }
        public int IncomingMessages { get; set; }
        public int LogonCount { get; set; }
        public int LogoutCount { get; set; }
        public int HeartbeatCount { get; set; }
        public int ErrorCount { get; set; }
        public DateTime FirstMessageTime { get; set; }
        public DateTime LastMessageTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, int> MessageTypeCount { get; set; } = new();
        public List<string> ErrorMessages { get; set; } = new();
    }
}
