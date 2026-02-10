using Terminal.Gui;
using NStack;

namespace ChinPakTools.DSE
{
    public class ProgramGUI
    {
        private static Window? mainWindow;
        private static TextView? outputView;
        private static TextField? inputField;
        private static Label? statusLabel;
        
        public static void Run(string[] args)
        {
            Application.Init();
            
            try
            {
                var top = Application.Top;
                
                // Main window
                mainWindow = new Window("ChinPak Universal FIX/FAST/ITCH Runner - DSE Edition")
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill()
                };
                
                // Menu bar
                var menu = new MenuBar(new MenuBarItem[]
                {
                    new MenuBarItem("_File", new MenuItem[]
                    {
                        new MenuItem("_Open Log", "", () => OpenLogFile()),
                        new MenuItem("_Save Output", "", () => SaveOutput()),
                        null!,
                        new MenuItem("_Quit", "", () => { Application.RequestStop(); })
                    }),
                    new MenuBarItem("_Tools", new MenuItem[]
                    {
                        new MenuItem("_FIX Decoder", "", () => ShowFixDecoder()),
                        new MenuItem("_FAST Decoder", "", () => ShowFastDecoder()),
                        new MenuItem("_ITCH Parser", "", () => ShowItchParser()),
                        null!,
                        new MenuItem("_Log Analyzer", "", () => ShowLogAnalyzer()),
                        new MenuItem("_Dictionary", "", () => ShowDictionary())
                    }),
                    new MenuBarItem("_Server", new MenuItem[]
                    {
                        new MenuItem("_FIX Server (DSE)", "", () => StartFixServer("DSE")),
                        new MenuItem("_FIX Server (CSE)", "", () => StartFixServer("CSE")),
                        new MenuItem("_FAST Server", "", () => StartFastServer()),
                        new MenuItem("_ITCH Server", "", () => StartItchServer()),
                        null!,
                        new MenuItem("_Stop All", "", () => StopAllServers())
                    }),
                    new MenuBarItem("_Help", new MenuItem[]
                    {
                        new MenuItem("_About", "", () => ShowAbout()),
                        new MenuItem("_Documentation", "", () => ShowDocumentation())
                    })
                });
                
                top.Add(menu);
                
                // Create main layout
                var contentFrame = new FrameView("Workspace")
                {
                    X = 0,
                    Y = 1,
                    Width = Dim.Fill(),
                    Height = Dim.Fill() - 3
                };
                
                // Output area
                outputView = new TextView
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill() - 3,
                    ReadOnly = true,
                    Text = GetWelcomeMessage()
                };
                
                // Input area
                var inputLabel = new Label("Input: ")
                {
                    X = 0,
                    Y = Pos.Bottom(outputView),
                    Width = 7
                };
                
                inputField = new TextField("")
                {
                    X = Pos.Right(inputLabel),
                    Y = Pos.Bottom(outputView),
                    Width = Dim.Fill() - 15
                };
                
                var executeBtn = new Button("Execute")
                {
                    X = Pos.Right(inputField) + 1,
                    Y = Pos.Bottom(outputView)
                };
                executeBtn.Clicked += ExecuteCommand;
                
                contentFrame.Add(outputView, inputLabel, inputField, executeBtn);
                mainWindow.Add(contentFrame);
                
                // Status bar
                statusLabel = new Label("Ready | FIX/FAST/ITCH Universal Runner")
                {
                    X = 0,
                    Y = Pos.Bottom(contentFrame),
                    Width = Dim.Fill(),
                    Height = 1,
                    ColorScheme = Colors.TopLevel
                };
                
                mainWindow.Add(statusLabel);
                
                // Quick action buttons
                var actionFrame = new FrameView("Quick Actions")
                {
                    X = 0,
                    Y = Pos.Bottom(contentFrame) + 1,
                    Width = Dim.Fill(),
                    Height = 2
                };
                
                var btnFix = new Button(2, 0, "FIX Decode");
                btnFix.Clicked += () => ShowFixDecoder();
                
                var btnFast = new Button(15, 0, "FAST Decode");
                btnFast.Clicked += () => ShowFastDecoder();
                
                var btnItch = new Button(29, 0, "ITCH Parse");
                btnItch.Clicked += () => ShowItchParser();
                
                var btnLog = new Button(42, 0, "Log Analyze");
                btnLog.Clicked += () => ShowLogAnalyzer();
                
                actionFrame.Add(btnFix, btnFast, btnItch, btnLog);
                mainWindow.Add(actionFrame);
                
                top.Add(mainWindow);
                
                Application.Run();
            }
            finally
            {
                Application.Shutdown();
            }
        }
        
        private static string GetWelcomeMessage()
        {
            return @"╔══════════════════════════════════════════════════════════════╗
║     ChinPak Universal FIX/FAST/ITCH Runner - DSE Edition     ║
╚══════════════════════════════════════════════════════════════╝

Welcome to the universal message protocol runner!

This tool supports:
  • FIX Protocol (DSE-BD, CSE-BD) - Message decoding and server
  • FAST Protocol - High-speed message encoding/decoding  
  • ITCH Protocol (NASDAQ ITCH 5.0) - Market data parsing

Available Operations:
  1. Decode FIX/FAST/ITCH messages
  2. Analyze session logs
  3. View protocol dictionaries
  4. Start protocol servers (FIX, FAST, ITCH)
  5. Send test messages to exchanges

Quick Start:
  • Use the menu bar to access different tools
  • Click Quick Action buttons below
  • Type commands in the input field

Status: Ready
";
        }
        
        private static void ExecuteCommand()
        {
            if (inputField == null || outputView == null) return;
            
            var command = inputField.Text?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(command))
                return;
            
            AppendOutput($"\n> {command}\n");
            
            // Simple command processing
            if (command.StartsWith("decode "))
            {
                var message = command.Substring(7);
                DecodeMessage(message);
            }
            else if (command.StartsWith("analyze "))
            {
                var file = command.Substring(8);
                AnalyzeLog(file);
            }
            else
            {
                AppendOutput("Unknown command. Use menu or Quick Actions.\n");
            }
            
            inputField.Text = "";
        }
        
        private static void ShowFixDecoder()
        {
            var dialog = new Dialog("FIX Message Decoder", 80, 15);
            
            var label = new Label("Enter FIX message (use | or SOH separator):") { X = 1, Y = 1 };
            var input = new TextField("") { X = 1, Y = 2, Width = Dim.Fill() - 2 };
            
            var btnDecode = new Button("Decode") { X = 1, Y = 4 };
            btnDecode.Clicked += () =>
            {
                var msg = input.Text?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    Application.RequestStop();
                    DecodeMessage(msg);
                }
            };
            
            var btnCancel = new Button("Cancel") { X = 12, Y = 4 };
            btnCancel.Clicked += () => Application.RequestStop();
            
            dialog.Add(label, input, btnDecode, btnCancel);
            Application.Run(dialog);
        }
        
        private static void ShowFastDecoder()
        {
            var dialog = new Dialog("FAST Message Decoder", 80, 15);
            
            var label = new Label("Enter FAST message (Base64 or Hex):") { X = 1, Y = 1 };
            var input = new TextField("") { X = 1, Y = 2, Width = Dim.Fill() - 2 };
            
            var formatLabel = new Label("Format:") { X = 1, Y = 3 };
            var radioBase64 = new RadioGroup(new NStack.ustring[] { "Base64", "Hex" }) { X = 10, Y = 3 };
            
            var btnDecode = new Button("Decode") { X = 1, Y = 5 };
            btnDecode.Clicked += () =>
            {
                var msg = input.Text?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    Application.RequestStop();
                    DecodeFastMessage(msg, radioBase64.SelectedItem == 0);
                }
            };
            
            var btnCancel = new Button("Cancel") { X = 12, Y = 5 };
            btnCancel.Clicked += () => Application.RequestStop();
            
            dialog.Add(label, input, formatLabel, radioBase64, btnDecode, btnCancel);
            Application.Run(dialog);
        }
        
        private static void ShowItchParser()
        {
            AppendOutput("\n[ITCH Parser]\n");
            AppendOutput("ITCH protocol parser - Feature coming soon!\n");
            AppendOutput("Will support NASDAQ ITCH 5.0 market data parsing.\n");
        }
        
        private static void ShowLogAnalyzer()
        {
            var dialog = new Dialog("Session Log Analyzer", 80, 12);
            
            var label = new Label("Enter log file path:") { X = 1, Y = 1 };
            var input = new TextField("") { X = 1, Y = 2, Width = Dim.Fill() - 2 };
            
            var btnAnalyze = new Button("Analyze") { X = 1, Y = 4 };
            btnAnalyze.Clicked += () =>
            {
                var file = input.Text?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(file))
                {
                    Application.RequestStop();
                    AnalyzeLog(file);
                }
            };
            
            var btnCancel = new Button("Cancel") { X = 13, Y = 4 };
            btnCancel.Clicked += () => Application.RequestStop();
            
            dialog.Add(label, input, btnAnalyze, btnCancel);
            Application.Run(dialog);
        }
        
        private static void ShowDictionary()
        {
            AppendOutput("\n[FIX Dictionary]\n");
            AppendOutput("Available commands:\n");
            AppendOutput("  field <tag>     - Lookup field by tag\n");
            AppendOutput("  message <type>  - Lookup message by type\n");
            AppendOutput("  search <term>   - Search fields\n");
            FixDictionaryViewer.ListAllMessages();
        }
        
        private static void StartFixServer(string exchange)
        {
            AppendOutput($"\n[Starting FIX Server for {exchange}]\n");
            AppendOutput($"This would start a FIX server for {exchange} exchange.\n");
            AppendOutput("Feature requires integration with FixProtocol.DSE/CSE modules.\n");
            UpdateStatus($"FIX Server ({exchange}) - Not Implemented");
        }
        
        private static void StartFastServer()
        {
            AppendOutput("\n[Starting FAST Server]\n");
            AppendOutput("This would start a FAST protocol server.\n");
            AppendOutput("Feature coming soon!\n");
            UpdateStatus("FAST Server - Not Implemented");
        }
        
        private static void StartItchServer()
        {
            AppendOutput("\n[Starting ITCH Server]\n");
            AppendOutput("This would start an ITCH market data server.\n");
            AppendOutput("Feature coming soon!\n");
            UpdateStatus("ITCH Server - Not Implemented");
        }
        
        private static void StopAllServers()
        {
            AppendOutput("\n[Stopping All Servers]\n");
            AppendOutput("All servers stopped.\n");
            UpdateStatus("Ready");
        }
        
        private static void DecodeMessage(string message)
        {
            try
            {
                var decoded = FixMessageDecoder.DecodeMessage(message);
                
                if (decoded.Success)
                {
                    AppendOutput($"\nMessage Type: {decoded.MessageType}\n");
                    AppendOutput(new string('─', 60) + "\n");
                    
                    foreach (var field in decoded.DecodedFields)
                    {
                        AppendOutput($"{field.Tag,6} | {field.Name,-25} | {field.Value}\n");
                    }
                    
                    AppendOutput(new string('─', 60) + "\n");
                    UpdateStatus($"Decoded {decoded.DecodedFields.Count} fields");
                }
                else
                {
                    AppendOutput($"Error: {decoded.Error}\n");
                    UpdateStatus("Decode failed");
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"Exception: {ex.Message}\n");
                UpdateStatus("Error");
            }
        }
        
        private static void DecodeFastMessage(string message, bool isBase64)
        {
            AppendOutput($"\n[FAST Decoder - {(isBase64 ? "Base64" : "Hex")}]\n");
            AppendOutput($"Input: {message}\n");
            AppendOutput("FAST decoding requires FastTools.Core integration.\n");
            AppendOutput("Feature coming soon!\n");
        }
        
        private static void AnalyzeLog(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    AppendOutput($"File not found: {filePath}\n");
                    UpdateStatus("File not found");
                    return;
                }
                
                var analyzer = new SessionLogAnalyzer();
                var stats = analyzer.AnalyzeLogFile(filePath);
                
                AppendOutput($"\n[Log Analysis: {Path.GetFileName(filePath)}]\n");
                AppendOutput(new string('═', 60) + "\n");
                AppendOutput($"Total Lines:        {stats.TotalLines:N0}\n");
                AppendOutput($"FIX Messages:       {stats.FixMessages:N0}\n");
                AppendOutput($"Outgoing:           {stats.OutgoingMessages:N0}\n");
                AppendOutput($"Incoming:           {stats.IncomingMessages:N0}\n");
                AppendOutput($"Errors:             {stats.ErrorCount:N0}\n");
                
                if (stats.FirstMessageTime != default)
                {
                    AppendOutput($"\nFirst Message:      {stats.FirstMessageTime:yyyy-MM-dd HH:mm:ss}\n");
                    AppendOutput($"Last Message:       {stats.LastMessageTime:yyyy-MM-dd HH:mm:ss}\n");
                    AppendOutput($"Duration:           {stats.Duration}\n");
                }
                
                if (stats.MessageTypeCount.Count > 0)
                {
                    AppendOutput("\nMessage Types:\n");
                    foreach (var kvp in stats.MessageTypeCount.OrderByDescending(x => x.Value).Take(10))
                    {
                        AppendOutput($"  {kvp.Key}: {kvp.Value:N0}\n");
                    }
                }
                
                AppendOutput(new string('═', 60) + "\n");
                UpdateStatus($"Analyzed {stats.FixMessages} messages");
            }
            catch (Exception ex)
            {
                AppendOutput($"Error: {ex.Message}\n");
                UpdateStatus("Analysis failed");
            }
        }
        
        private static void OpenLogFile()
        {
            var dialog = new OpenDialog("Open Log File", "Open log file for analysis");
            Application.Run(dialog);
            
            if (!dialog.Canceled && dialog.FilePath != null)
            {
                AnalyzeLog(dialog.FilePath.ToString() ?? "");
            }
        }
        
        private static void SaveOutput()
        {
            var dialog = new SaveDialog("Save Output", "Save output to file");
            Application.Run(dialog);
            
            if (!dialog.Canceled && dialog.FilePath != null)
            {
                try
                {
                    var content = outputView?.Text?.ToString() ?? "";
                    File.WriteAllText(dialog.FilePath.ToString() ?? "", content);
                    UpdateStatus($"Saved to {Path.GetFileName(dialog.FilePath.ToString())}");
                }
                catch (Exception ex)
                {
                    MessageBox.ErrorQuery("Error", $"Failed to save: {ex.Message}", "OK");
                }
            }
        }
        
        private static void ShowAbout()
        {
            MessageBox.Query("About ChinPak Tools", 
                @"ChinPak Universal FIX/FAST/ITCH Runner
Version 1.0.0

A universal message protocol runner supporting:
  • FIX Protocol (DSE-BD, CSE-BD)
  • FAST Protocol  
  • ITCH Protocol (NASDAQ ITCH 5.0)

Features:
  • Message decoding and encoding
  • Session log analysis
  • Protocol dictionaries
  • Server capabilities

License: MIT
", "OK");
        }
        
        private static void ShowDocumentation()
        {
            MessageBox.Query("Documentation", 
                @"ChinPak Tools - Quick Guide

Menu Bar:
  File   - Open logs, save output
  Tools  - Decoders and analyzers
  Server - Start/stop protocol servers
  Help   - About and documentation

Quick Actions:
  Use buttons for common tasks

Input Commands:
  decode <message>  - Decode FIX message
  analyze <file>    - Analyze log file

For full documentation, see README.md
", "OK");
        }
        
        private static void AppendOutput(string text)
        {
            if (outputView != null)
            {
                outputView.Text += text;
                outputView.MoveEnd();
            }
        }
        
        private static void UpdateStatus(string status)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = $"Status: {status} | FIX/FAST/ITCH Universal Runner";
            }
        }
    }
}
