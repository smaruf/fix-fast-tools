using System;
using System.IO;
using FastTools.Core.Services;

namespace FastTools.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════╗");
            Console.WriteLine("║   FAST Message Decoder - CLI Tool    ║");
            Console.WriteLine("╚═══════════════════════════════════════╝");
            Console.WriteLine();

            var decoder = new FastMessageDecoder();
            
            // Try to load template from current directory or parent
            string templatePath = "FAST_TEMPLATE.xml";
            if (!File.Exists(templatePath))
                templatePath = Path.Combine("..", "Tools", "FAST_TEMPLATE.xml");
            
            if (File.Exists(templatePath))
            {
                decoder.LoadTemplateMap(templatePath);
                Console.WriteLine($"✓ Loaded template map from {templatePath}");
            }
            else
            {
                Console.WriteLine("⚠ Template file not found, decoding without template names");
            }
            Console.WriteLine();

            if (args.Length == 0)
            {
                ShowHelp();
                RunInteractiveMode(decoder);
                return;
            }

            ProcessArguments(args, decoder);
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  FastTools.CLI [command] [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  --base64 <string>        Decode base64 encoded message");
            Console.WriteLine("  --hex <string>           Decode hex encoded message");
            Console.WriteLine("  --file <path>            Decode binary file");
            Console.WriteLine("  --json <path>            Decode JSON file with FAST messages");
            Console.WriteLine("  --interactive            Start interactive mode (default)");
            Console.WriteLine("  --help                   Show this help");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --template-id <id>       Specify template ID for decoding");
            Console.WriteLine("  --export <path>          Export results to JSON file");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  FastTools.CLI --base64 SGVsbG8gV29ybGQ=");
            Console.WriteLine("  FastTools.CLI --hex \"A1 B2 C3 D4\"");
            Console.WriteLine("  FastTools.CLI --file message.dat --template-id 14");
            Console.WriteLine("  FastTools.CLI --json messages.json --export results.json");
            Console.WriteLine();
        }

        static void RunInteractiveMode(FastMessageDecoder decoder)
        {
            Console.WriteLine("Interactive Mode");
            Console.WriteLine("────────────────");
            
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("Select an option:");
                Console.WriteLine("  1. Decode Base64");
                Console.WriteLine("  2. Decode Hex");
                Console.WriteLine("  3. Decode Binary File");
                Console.WriteLine("  4. Decode JSON File");
                Console.WriteLine("  5. Help");
                Console.WriteLine("  0. Exit");
                Console.Write("> ");
                
                var choice = Console.ReadLine()?.Trim();
                
                switch (choice)
                {
                    case "1":
                        DecodeBase64Interactive(decoder);
                        break;
                    case "2":
                        DecodeHexInteractive(decoder);
                        break;
                    case "3":
                        DecodeFileInteractive(decoder);
                        break;
                    case "4":
                        DecodeJsonFileInteractive(decoder);
                        break;
                    case "5":
                        ShowHelp();
                        break;
                    case "0":
                        Console.WriteLine("Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        static void DecodeBase64Interactive(FastMessageDecoder decoder)
        {
            Console.Write("Enter Base64 string: ");
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("❌ No input provided");
                return;
            }

            Console.Write("Template ID (optional, press Enter to skip): ");
            var templateIdStr = Console.ReadLine();
            int? templateId = null;
            if (int.TryParse(templateIdStr, out int tid))
                templateId = tid;

            try
            {
                var bytes = Convert.FromBase64String(input);
                var result = decoder.DecodeBinary(bytes, templateId);
                DisplayResult(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static void DecodeHexInteractive(FastMessageDecoder decoder)
        {
            Console.Write("Enter Hex string: ");
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("❌ No input provided");
                return;
            }

            Console.Write("Template ID (optional, press Enter to skip): ");
            var templateIdStr = Console.ReadLine();
            int? templateId = null;
            if (int.TryParse(templateIdStr, out int tid))
                templateId = tid;

            try
            {
                var hex = input.Replace(" ", "").Replace("-", "");
                var bytes = new List<byte>();
                for (int i = 0; i < hex.Length; i += 2)
                {
                    bytes.Add(Convert.ToByte(hex.Substring(i, 2), 16));
                }
                var result = decoder.DecodeBinary(bytes.ToArray(), templateId);
                DisplayResult(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static void DecodeFileInteractive(FastMessageDecoder decoder)
        {
            Console.Write("Enter file path: ");
            var filePath = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine("❌ File not found");
                return;
            }

            Console.Write("Template ID (optional, press Enter to skip): ");
            var templateIdStr = Console.ReadLine();
            int? templateId = null;
            if (int.TryParse(templateIdStr, out int tid))
                templateId = tid;

            try
            {
                var bytes = File.ReadAllBytes(filePath);
                var result = decoder.DecodeBinary(bytes, templateId);
                DisplayResult(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static void DecodeJsonFileInteractive(FastMessageDecoder decoder)
        {
            Console.Write("Enter JSON file path: ");
            var filePath = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine("❌ File not found");
                return;
            }

            try
            {
                var results = decoder.DecodeJsonFile(filePath);
                Console.WriteLine($"\n✓ Decoded {results.Count} messages\n");
                
                for (int i = 0; i < Math.Min(results.Count, 5); i++)
                {
                    Console.WriteLine($"─── Message {i + 1} ───");
                    DisplayResult(results[i]);
                }
                
                if (results.Count > 5)
                {
                    Console.WriteLine($"\n... and {results.Count - 5} more messages");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static void ProcessArguments(string[] args, FastMessageDecoder decoder)
        {
            try
            {
                switch (args[0].ToLower())
                {
                    case "--base64":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("❌ Usage: --base64 <base64string>");
                            return;
                        }
                        var bytes = Convert.FromBase64String(args[1]);
                        var result = decoder.DecodeBinary(bytes);
                        DisplayResult(result);
                        break;

                    case "--hex":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("❌ Usage: --hex <hexstring>");
                            return;
                        }
                        var hex = args[1].Replace(" ", "").Replace("-", "");
                        var hexBytes = new List<byte>();
                        for (int i = 0; i < hex.Length; i += 2)
                        {
                            hexBytes.Add(Convert.ToByte(hex.Substring(i, 2), 16));
                        }
                        var hexResult = decoder.DecodeBinary(hexBytes.ToArray());
                        DisplayResult(hexResult);
                        break;

                    case "--file":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("❌ Usage: --file <path>");
                            return;
                        }
                        if (!File.Exists(args[1]))
                        {
                            Console.WriteLine($"❌ File not found: {args[1]}");
                            return;
                        }
                        var fileBytes = File.ReadAllBytes(args[1]);
                        var fileResult = decoder.DecodeBinary(fileBytes);
                        DisplayResult(fileResult);
                        break;

                    case "--json":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("❌ Usage: --json <path>");
                            return;
                        }
                        if (!File.Exists(args[1]))
                        {
                            Console.WriteLine($"❌ File not found: {args[1]}");
                            return;
                        }
                        var jsonResults = decoder.DecodeJsonFile(args[1]);
                        Console.WriteLine($"\n✓ Decoded {jsonResults.Count} messages\n");
                        foreach (var msg in jsonResults.Take(5))
                        {
                            DisplayResult(msg);
                            Console.WriteLine();
                        }
                        if (jsonResults.Count > 5)
                        {
                            Console.WriteLine($"... and {jsonResults.Count - 5} more messages");
                        }
                        break;

                    case "--help":
                    case "-h":
                        ShowHelp();
                        break;

                    case "--interactive":
                    case "-i":
                        RunInteractiveMode(decoder);
                        break;

                    default:
                        Console.WriteLine($"❌ Unknown command: {args[0]}");
                        Console.WriteLine("Use --help to see available commands");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static void DisplayResult(FastTools.Core.Models.DecodedMessage result)
        {
            Console.WriteLine("┌─────────────────────────────────────┐");
            
            if (result.TemplateId > 0)
            {
                Console.WriteLine($"│ Template ID: {result.TemplateId}");
                if (!string.IsNullOrEmpty(result.TemplateName))
                    Console.WriteLine($"│ Template Name: {result.TemplateName}");
            }
            
            if (!string.IsNullOrEmpty(result.MsgType))
                Console.WriteLine($"│ Message Type: {result.MsgType}");
            
            if (!string.IsNullOrEmpty(result.MsgName))
                Console.WriteLine($"│ Message Name: {result.MsgName}");
            
            if (result.RawBytes != null && result.RawBytes.Length > 0)
                Console.WriteLine($"│ Size: {result.RawBytes.Length} bytes");
            
            Console.WriteLine("└─────────────────────────────────────┘");
            
            if (!string.IsNullOrEmpty(result.HexRepresentation))
            {
                Console.WriteLine("\nHex Representation:");
                Console.WriteLine(result.HexRepresentation);
            }
            
            if (result.DetectedStrings?.Count > 0)
            {
                Console.WriteLine("\nDetected Strings:");
                foreach (var str in result.DetectedStrings)
                {
                    Console.WriteLine($"  • {str}");
                }
            }
            
            if (result.StopBitIntegers?.Count > 0)
            {
                Console.WriteLine("\nStop-bit Integers:");
                Console.WriteLine($"  {string.Join(", ", result.StopBitIntegers)}");
            }
            
            if (result.Fields?.Count > 0)
            {
                Console.WriteLine("\nFields:");
                foreach (var field in result.Fields)
                {
                    Console.WriteLine($"  {field.Key}: {field.Value}");
                }
            }
        }
    }
}

