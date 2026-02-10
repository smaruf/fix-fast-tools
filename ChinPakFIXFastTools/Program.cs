using System;
using ChinPakFIXFastTools;

Console.WriteLine("╔══════════════════════════════════════════════╗");
Console.WriteLine("║       ChinPak FIX Tools for DSE-BD          ║");
Console.WriteLine("║   Dhaka Stock Exchange - Bangladesh         ║");
Console.WriteLine("╚══════════════════════════════════════════════╝");
Console.WriteLine();

bool running = true;

while (running)
{
    Console.WriteLine("\n═════════════════════════════════════════════");
    Console.WriteLine("Main Menu:");
    Console.WriteLine("═════════════════════════════════════════════");
    Console.WriteLine("  1. Decode FIX Message");
    Console.WriteLine("  2. Analyze Session Log");
    Console.WriteLine("  3. FIX Dictionary Viewer");
    Console.WriteLine("  4. Launch GUI Interface");
    Console.WriteLine("  5. About");
    Console.WriteLine("  0. Exit");
    Console.WriteLine("═════════════════════════════════════════════");
    Console.Write("Enter your choice: ");
    
    var choice = Console.ReadLine()?.Trim();
    
    switch (choice)
    {
        case "1":
            DecodeFixMessage();
            break;
            
        case "2":
            AnalyzeSessionLog();
            break;
            
        case "3":
            DictionaryViewer();
            break;
            
        case "4":
            Console.WriteLine("\n🚀 GUI Interface is now available in CommonGUI project");
            Console.WriteLine("   Run: cd CommonGUI && dotnet run");
            break;
            
        case "5":
            ShowAbout();
            break;
            
        case "0":
            running = false;
            Console.WriteLine("\n👋 Thank you for using ChinPak FIX Tools!");
            break;
            
        default:
            Console.WriteLine("❌ Invalid choice. Please try again.");
            break;
    }
}

static void DecodeFixMessage()
{
    Console.WriteLine("\n╔═══ FIX Message Decoder ═══╗");
    Console.WriteLine("Enter FIX message (use | as field separator or paste raw message):");
    Console.Write("> ");
    
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        Console.WriteLine("⚠️  No input provided");
        return;
    }
    
    var decoded = FixMessageDecoder.DecodeMessage(input);
    decoded.PrintToConsole();
}

static void AnalyzeSessionLog()
{
    Console.WriteLine("\n╔═══ Session Log Analyzer ═══╗");
    Console.Write("Enter log file path: ");
    
    var filePath = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(filePath))
    {
        Console.WriteLine("⚠️  No file path provided");
        return;
    }
    
    try
    {
        var analyzer = new SessionLogAnalyzer();
        var stats = analyzer.AnalyzeLogFile(filePath);
        analyzer.PrintStats(stats);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
    }
}

static void DictionaryViewer()
{
    Console.WriteLine("\n╔═══ FIX Dictionary Viewer ═══╗");
    Console.WriteLine("  1. Lookup Field by Tag");
    Console.WriteLine("  2. Lookup Message by Type");
    Console.WriteLine("  3. Search Fields");
    Console.WriteLine("  4. List All Messages");
    Console.WriteLine("  0. Back to Main Menu");
    Console.Write("\nEnter your choice: ");
    
    var choice = Console.ReadLine()?.Trim();
    
    switch (choice)
    {
        case "1":
            Console.Write("Enter field tag: ");
            if (int.TryParse(Console.ReadLine(), out var tag))
                FixDictionaryViewer.DisplayFieldInfo(tag);
            else
                Console.WriteLine("❌ Invalid tag");
            break;
            
        case "2":
            Console.Write("Enter message type (e.g., D, 8, A): ");
            var msgType = Console.ReadLine()?.Trim()?.ToUpper();
            if (!string.IsNullOrEmpty(msgType))
                FixDictionaryViewer.DisplayMessageInfo(msgType);
            else
                Console.WriteLine("❌ Invalid message type");
            break;
            
        case "3":
            Console.Write("Enter search term: ");
            var searchTerm = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(searchTerm))
                FixDictionaryViewer.SearchFields(searchTerm);
            else
                Console.WriteLine("❌ Invalid search term");
            break;
            
        case "4":
            FixDictionaryViewer.ListAllMessages();
            break;
            
        case "0":
            return;
            
        default:
            Console.WriteLine("❌ Invalid choice");
            break;
    }
}

static void ShowAbout()
{
    Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║              ChinPak FIX Tools - DSE Edition              ║");
    Console.WriteLine("╠═══════════════════════════════════════════════════════════╣");
    Console.WriteLine("║  Version:     1.0.0                                       ║");
    Console.WriteLine("║  Exchange:    Dhaka Stock Exchange (DSE-BD)               ║");
    Console.WriteLine("║  Protocol:    FIX 4.4                                     ║");
    Console.WriteLine("║                                                           ║");
    Console.WriteLine("║  Features:                                                ║");
    Console.WriteLine("║    • FIX Message Decoder with field translation          ║");
    Console.WriteLine("║    • Session Log Analyzer with statistics                ║");
    Console.WriteLine("║    • FIX Dictionary Viewer (fields & messages)           ║");
    Console.WriteLine("║    • Support for Bangladesh stock exchanges               ║");
    Console.WriteLine("║                                                           ║");
    Console.WriteLine("║  Tools:                                                   ║");
    Console.WriteLine("║    • CLI Interface (current)                              ║");
    Console.WriteLine("║    • GUI Interface (use ProgramGUI)                       ║");
    Console.WriteLine("║                                                           ║");
    Console.WriteLine("║  License:     MIT                                         ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
}
