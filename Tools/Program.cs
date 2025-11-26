using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Collections.Generic;
using System.Linq;

namespace Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("FAST Message Decoder");
                Console.WriteLine("===================");

                // Load template mapping (id -> name) from FAST_TEMPLATE.xml for reference only
                var templateMap = LoadTemplateMap("FAST_TEMPLATE.xml");

                if (args.Length > 0)
                {
                    // CLI modes
                    switch (args[0])
                    {
                        case "--base64":
                            if (args.Length < 2) { Console.WriteLine("Usage: --base64 <base64string>"); break; }
                            var bytes = Convert.FromBase64String(args[1]);
                            ParseSingleBinary(bytes, templateMap);
                            return;

                        case "--hex":
                            if (args.Length < 2) { Console.WriteLine("Usage: --hex <hexstring>"); break; }
                            var hex = args[1].Replace(" ", "");
                            var b = new List<byte>();
                            for (int i = 0; i < hex.Length; i += 2) b.Add(Convert.ToByte(hex.Substring(i, 2), 16));
                            ParseSingleBinary(b.ToArray(), templateMap);
                            return;

                        case "--file":
                            if (args.Length < 2) { Console.WriteLine("Usage: --file <path>"); break; }
                            if (!File.Exists(args[1])) { Console.WriteLine("File not found: " + args[1]); break; }
                            var data = File.ReadAllBytes(args[1]);
                            ParseSingleBinary(data, templateMap);
                            return;

                        case "--json":
                            if (args.Length < 2) { Console.WriteLine("Usage: --json <path>"); break; }
                            ProcessJsonFile(args[1]);
                            return;

                        default:
                            Console.WriteLine("Unknown option. Supported: --base64 --hex --file --json");
                            return;
                    }
                }

                Console.WriteLine("Processing FAST messages from JSON file...");
                ProcessJsonFile("FastIncomingMessage251126-ACI.json");

                Console.WriteLine("\nProcessing log file...");
                ProcessLogFile("sample_messages.dat");

                Console.WriteLine("\nDecoding completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\nExiting...");
            // Removed Console.ReadKey() to allow non-interactive runs and CI-friendly output
        }

        static Dictionary<int, string> LoadTemplateMap(string xmlFile)
        {
            var map = new Dictionary<int, string>();
            if (!File.Exists(xmlFile))
                return map;

            try
            {
                var doc = new XmlDocument();
                doc.Load(xmlFile);
                var nodes = doc.GetElementsByTagName("template");
                foreach (XmlElement el in nodes)
                {
                    if (el.HasAttribute("id") )
                    {
                        if (int.TryParse(el.GetAttribute("id"), out int id))
                        {
                            var name = el.GetAttribute("name");
                            if (string.IsNullOrEmpty(name)) name = el.GetAttribute("dictionary");
                            map[id] = name ?? (el.LocalName ?? "template");
                        }
                    }
                }
            }
            catch { }

            return map;
        }

        static void ParseSingleBinary(byte[] data, Dictionary<int, string> templateMap)
        {
            Console.WriteLine($"Parsing single binary blob ({data.Length} bytes)");
            Console.WriteLine("Hex:");
            Console.WriteLine(BitConverter.ToString(data).Replace("-", " "));

            // try to read first stop-bit encoded integer (common for template id)
            int pos = 0;
            var (value, read) = ReadStopBitUInt(data, 0);
            if (read > 0)
            {
                Console.WriteLine($"First stop-bit uint: {value} (read {read} byte(s))");
                if (templateMap.TryGetValue(value, out var tname))
                    Console.WriteLine($"Template id {value} -> {tname}");
            }
            else
            {
                Console.WriteLine("No leading stop-bit integer found; trying heuristics...");
            }

            // print strings found
            var sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                if (b >= 32 && b <= 126)
                {
                    sb.Append((char)b);
                }
                else
                {
                    if (sb.Length >= 3)
                    {
                        Console.WriteLine("Detected ASCII string: '" + sb + "'");
                    }
                    sb.Clear();
                }
            }
            if (sb.Length >= 3) Console.WriteLine("Detected ASCII string: '" + sb + "'");

            // show first stop-bit ints sequence
            Console.WriteLine("First few stop-bit integers:");
            int p = 0;
            for (int i = 0; i < 5 && p < data.Length; i++)
            {
                var (v, r) = ReadStopBitUInt(data, p);
                if (r == 0) break;
                Console.WriteLine($"  {i+1}: {v} (at {p}, {r} byte(s))");
                p += r;
            }
        }

        // returns (value, bytesRead)
        static (int, int) ReadStopBitUInt(byte[] data, int offset)
        {
            long value = 0;
            int read = 0;
            for (int i = offset; i < data.Length; i++)
            {
                byte b = data[i];
                // cast masked byte to long to avoid sign-extension / bitwise warnings
                value = (value << 7) | ((long)(b & 0x7F));
                read++;
                if ((b & 0x80) != 0)
                {
                    return ((int)value, read);
                }
                // safety: stop after 5 bytes
                if (read >= 5) break;
            }
            return (0, 0);
        }

        static void ProcessJsonFile(string jsonFile)
        {
            if (!File.Exists(jsonFile))
            {
                Console.WriteLine($"JSON file '{jsonFile}' not found.");
                return;
            }

            string jsonContent = File.ReadAllText(jsonFile);
            var messages = JsonSerializer.Deserialize<FastMessage[]>(jsonContent) ?? Array.Empty<FastMessage>();
            
            Console.WriteLine($"Found {messages.Length} FAST messages in JSON file.");

            for (int i = 0; i < Math.Min(messages.Length, 3); i++)
            {
                var msg = messages[i];
                Console.WriteLine($"\n--- Message {i + 1}: {msg.MsgName} ---");
                Console.WriteLine($"Template ID: {msg.TemplateId}");
                Console.WriteLine($"Message Type: {msg.MsgType}");
                Console.WriteLine($"Timestamp: {GetJsonValue(msg.SendingDateTimeUtc)}");
                
                // Decode Base64 FAST message
                byte[] rawBytes = Convert.FromBase64String(msg.RawMsg?.Base64 ?? string.Empty);
                Console.WriteLine($"Raw size: {rawBytes.Length} bytes");
                
                // Parse FAST message
                ParseFastMessage(rawBytes, msg.TemplateId);
                
                // Show parsed content
                if (!string.IsNullOrEmpty(msg.MsgText))
                {
                    ShowParsedContent(msg.MsgText);
                }
            }
        }

        static void ParseFastMessage(byte[] data, int templateId)
        {
            Console.WriteLine("FAST Message Analysis:");
            
            if (data.Length == 0) return;
            
            // Basic FAST parsing - look for template ID and common patterns
            int pos = 0;
            
            // Template ID is usually the first field
            if (data[0] == templateId)
            {
                Console.WriteLine($"  ✓ Template ID matches: {templateId}");
                pos = 1;
            }
            
            // Look for string fields (SecurityDefinition fields)
            while (pos < data.Length)
            {
                byte b = data[pos];
                
                // Check for string start (printable characters)
                if (b >= 32 && b <= 126)
                {
                    StringBuilder str = new StringBuilder();
                    while (pos < data.Length && data[pos] >= 32 && data[pos] <= 126 && data[pos] != 0x80)
                    {
                        str.Append((char)data[pos]);
                        pos++;
                    }
                    if (str.Length > 2)
                    {
                        Console.WriteLine($"  String field: '{str}'");
                    }
                }
                
                // Skip stop bit encoded integers
                if ((b & 0x80) != 0)
                {
                    Console.WriteLine($"  Integer/Stop bit at pos {pos}: {b & 0x7F}");
                }
                
                pos++;
                
                // Limit output
                if (pos > 50) break;
            }
        }

        static void ShowParsedContent(string msgText)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<JsonElement>(msgText);
                Console.WriteLine("Parsed SecurityDefinition fields:");
                
                if (parsed.TryGetProperty("ApplID", out var applId))
                    Console.WriteLine($"  ApplID: {GetJsonValue(applId)}");
                
                if (parsed.TryGetProperty("Symbol", out var symbol))
                    Console.WriteLine($"  Symbol: {GetJsonValue(symbol)}");
                
                if (parsed.TryGetProperty("SecurityType", out var secType))
                    Console.WriteLine($"  SecurityType: {GetJsonValue(secType)}");
                
                if (parsed.TryGetProperty("SecurityReqID", out var reqId))
                    Console.WriteLine($"  SecurityReqID: {GetJsonValue(reqId)}");
                
                if (parsed.TryGetProperty("SendingTime", out var sendTime))
                    Console.WriteLine($"  SendingTime: {GetJsonValue(sendTime)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing message text: {ex.Message}");
            }
        }

        static string GetJsonValue(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("Value", out var value))
                return value.ToString();
            return element.ToString();
        }

        static void ProcessLogFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"Log file '{fileName}' not found.");
                return;
            }

            string[] lines = File.ReadAllLines(fileName);
            Console.WriteLine($"Processing {lines.Length} log entries...");

            int count = 0;
            foreach (string line in lines)
            {
                if (line.Contains("INPUT :") || line.Contains("OUTPUT :"))
                {
                    count++;
                    Console.WriteLine($"\nLog entry {count}:");
                    
                    string[] parts = line.Split(new string[] { " : " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        string timestamp = parts[0];
                        string data = parts[1];
                        
                        Console.WriteLine($"  Time: {timestamp}");
                        Console.WriteLine($"  Data length: {data.Length} chars");
                        
                        // Look for FAST protocol indicators
                        if (data.Contains("FAST"))
                            Console.WriteLine("  ✓ Contains FAST protocol marker");
                        if (data.Contains("fcsl"))
                            Console.WriteLine("  ✓ Contains application ID 'fcsl'");
                        
                        // Show first few readable characters
                        StringBuilder readable = new StringBuilder();
                        foreach (char c in data.Take(20))
                        {
                            if (c >= 32 && c <= 126)
                                readable.Append(c);
                            else
                                readable.Append('.');
                        }
                        Console.WriteLine($"  Preview: {readable}...");
                    }
                    
                    if (count >= 3) break;
                }
            }
        }
    }

    public class FastMessage
    {
        public string Channel { get; set; }
        public int PacketNum { get; set; }
        // use JsonElement to accept any JSON type without throwing during deserialization
        public System.Text.Json.JsonElement SendingDateTimeUtc { get; set; }
        public int TemplateId { get; set; }
        public string MsgType { get; set; }
        public string MsgName { get; set; }
        public string ServerId { get; set; }
        public string MsgText { get; set; }
        public RawMsgData RawMsg { get; set; }
        public System.Text.Json.JsonElement CreatedDateTimeUtc { get; set; }
    }

    public class RawMsgData
    {
        public string Base64 { get; set; }
        public string SubType { get; set; }
    }
}