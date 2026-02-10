using Terminal.Gui;
using NStack;
using ChinPakFIXFastTools;

namespace CommonGUI
{
    public class ProgramGUI
    {
        private static Window? mainWindow;
        private static TextView? outputView;
        private static TextField? inputField;
        private static Label? statusLabel;
        private static TradingSession? tradingSession;
        
        public static void Main(string[] args)
        {
            Run(args);
        }
        
        public static void Run(string[] args)
        {
            Application.Init();
            
            // Initialize trading session
            tradingSession = new TradingSession();
            tradingSession.OnStatusChanged += (status) => UpdateStatus(status);
            tradingSession.OnLogMessage += (msg) => AppendOutput($"[TRADING] {msg}\n");
            tradingSession.OnExecutionReport += (report) =>
            {
                AppendOutput($"\n[EXECUTION REPORT]\n");
                AppendOutput($"  Time: {report.Timestamp:HH:mm:ss}\n");
                AppendOutput($"  OrderID: {report.OrderID}\n");
                AppendOutput($"  Symbol: {report.Symbol}\n");
                AppendOutput($"  Side: {report.Side}\n");
                AppendOutput($"  Status: {report.OrdStatus}\n");
                AppendOutput($"  Exec Type: {report.ExecType}\n");
                AppendOutput($"  Qty: {report.OrderQty}, Filled: {report.CumQty}, Remaining: {report.LeavesQty}\n");
                if (report.LastPx.HasValue)
                    AppendOutput($"  Last Price: {report.LastPx}, Last Qty: {report.LastQty}\n");
                if (!string.IsNullOrEmpty(report.Text))
                    AppendOutput($"  Text: {report.Text}\n");
                AppendOutput(new string('─', 60) + "\n");
            };
            
            try
            {
                var top = Application.Top;
                
                // Main window
                mainWindow = new Window("ChinPak Universal FIX/FAST/ITCH Runner")
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
                        new MenuItem("─────", "", null) { CanExecute = () => false }, // Separator
                        new MenuItem("_Quit", "", () => { Application.RequestStop(); })
                    }),
                    new MenuBarItem("_Tools", new MenuItem[]
                    {
                        new MenuItem("_FIX Decoder", "", () => ShowFixDecoder()),
                        new MenuItem("_FAST Decoder", "", () => ShowFastDecoder()),
                        new MenuItem("_ITCH Parser", "", () => ShowItchParser()),
                        new MenuItem("─────", "", null) { CanExecute = () => false }, // Separator
                        new MenuItem("_Log Analyzer", "", () => ShowLogAnalyzer()),
                        new MenuItem("_Dictionary", "", () => ShowDictionary())
                    }),
                    new MenuBarItem("T_rading", new MenuItem[]
                    {
                        new MenuItem("Session _Login", "", () => ShowSessionLogin()),
                        new MenuItem("_Place Order", "", () => ShowPlaceOrder()),
                        new MenuItem("View _Execution Reports", "", () => ShowExecutionReports()),
                        new MenuItem("_Market Data Feed", "", () => ShowMarketDataFeed()),
                        new MenuItem("─────", "", null) { CanExecute = () => false }, // Separator
                        new MenuItem("Session Log_out", "", () => ShowSessionLogout())
                    }),
                    new MenuBarItem("_Server", new MenuItem[]
                    {
                        new MenuItem("_FIX Server (DSE)", "", () => StartFixServer("DSE")),
                        new MenuItem("_FIX Server (CSE)", "", () => StartFixServer("CSE")),
                        new MenuItem("_FAST Server", "", () => StartFastServer()),
                        new MenuItem("_ITCH Server", "", () => StartItchServer()),
                        new MenuItem("─────", "", null) { CanExecute = () => false }, // Separator
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
║         ChinPak Universal FIX/FAST/ITCH Runner               ║
╚══════════════════════════════════════════════════════════════╝

Welcome to the universal message protocol runner!

This tool supports:
  • FIX Protocol (DSE-BD, CSE-BD) - Message decoding and server
  • FAST Protocol - High-speed message encoding/decoding  
  • ITCH Protocol (NASDAQ ITCH 5.0) - Market data parsing
  • Live Trading - FIX session management and order placement

Available Operations:
  1. Decode FIX/FAST/ITCH messages
  2. Analyze session logs
  3. View protocol dictionaries
  4. Start protocol servers (FIX, FAST, ITCH)
  5. Connect to exchanges and place orders
  6. Monitor execution reports and market data

Trading Features:
  • Session Login/Logout to DSE or CSE
  • Place Buy/Sell orders (Market/Limit)
  • View execution reports
  • Market data feed (coming soon)

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
            var formatRadioGroup = new RadioGroup(new NStack.ustring[] { "Base64", "Hex" }) { X = 10, Y = 3 };
            
            var btnDecode = new Button("Decode") { X = 1, Y = 5 };
            btnDecode.Clicked += () =>
            {
                var msg = input.Text?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    Application.RequestStop();
                    DecodeFastMessage(msg, formatRadioGroup.SelectedItem == 0);
                }
            };
            
            var btnCancel = new Button("Cancel") { X = 12, Y = 5 };
            btnCancel.Clicked += () => Application.RequestStop();
            
            dialog.Add(label, input, formatLabel, formatRadioGroup, btnDecode, btnCancel);
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
        
        // Trading Menu Functions
        
        private static void ShowSessionLogin()
        {
            var dialog = new Dialog("Session Login", 60, 18);
            
            var exchangeLabel = new Label("Select Exchange:") { X = 1, Y = 1 };
            var exchangeRadio = new RadioGroup(new NStack.ustring[] { "DSE", "CSE" }) { X = 1, Y = 2 };
            
            var configLabel = new Label("Configuration File:") { X = 1, Y = 4 };
            var configField = new TextField("") { X = 1, Y = 5, Width = Dim.Fill() - 2 };
            
            var browseBtn = new Button("Browse...") { X = 1, Y = 6 };
            browseBtn.Clicked += () =>
            {
                var fileDialog = new OpenDialog("Select FIX Configuration", "Select configuration file");
                fileDialog.AllowedFileTypes = new[] { ".cfg" };
                Application.Run(fileDialog);
                
                if (!fileDialog.Canceled && fileDialog.FilePath != null)
                {
                    configField.Text = fileDialog.FilePath.ToString();
                }
            };
            
            var infoLabel = new Label("Note: Configuration file should contain FIX session settings") 
            { 
                X = 1, 
                Y = 8,
                Width = Dim.Fill() - 2
            };
            
            var btnConnect = new Button("Connect") { X = 1, Y = 10 };
            btnConnect.Clicked += () =>
            {
                var exchange = exchangeRadio.SelectedItem == 0 ? "DSE" : "CSE";
                var configPath = configField.Text?.ToString() ?? "";
                
                if (string.IsNullOrWhiteSpace(configPath))
                {
                    MessageBox.ErrorQuery("Error", "Please select a configuration file", "OK");
                    return;
                }
                
                Application.RequestStop();
                
                AppendOutput($"\n[Connecting to {exchange}]\n");
                AppendOutput($"Config: {configPath}\n");
                
                if (tradingSession != null)
                {
                    bool success = tradingSession.Connect(exchange, configPath);
                    if (success)
                    {
                        AppendOutput("Connection initiated. Waiting for logon...\n");
                    }
                    else
                    {
                        AppendOutput("Connection failed. Check configuration and logs.\n");
                        MessageBox.ErrorQuery("Error", "Failed to connect. Check configuration file.", "OK");
                    }
                }
            };
            
            var btnCancel = new Button("Cancel") { X = 13, Y = 10 };
            btnCancel.Clicked += () => Application.RequestStop();
            
            dialog.Add(exchangeLabel, exchangeRadio, configLabel, configField, browseBtn, infoLabel, btnConnect, btnCancel);
            Application.Run(dialog);
        }
        
        private static void ShowPlaceOrder()
        {
            if (tradingSession == null || !tradingSession.IsConnected)
            {
                MessageBox.ErrorQuery("Error", "Not connected to any exchange. Please login first.", "OK");
                return;
            }
            
            var dialog = new Dialog("Place Order", 70, 20);
            
            var symbolLabel = new Label("Symbol:") { X = 1, Y = 1 };
            var symbolField = new TextField("") { X = 20, Y = 1, Width = 20 };
            
            var sideLabel = new Label("Side:") { X = 1, Y = 3 };
            var sideRadio = new RadioGroup(new NStack.ustring[] { "BUY", "SELL" }) { X = 20, Y = 3 };
            
            var qtyLabel = new Label("Quantity:") { X = 1, Y = 5 };
            var qtyField = new TextField("") { X = 20, Y = 5, Width = 20 };
            
            var orderTypeLabel = new Label("Order Type:") { X = 1, Y = 7 };
            var orderTypeRadio = new RadioGroup(new NStack.ustring[] { "Market", "Limit" }) { X = 20, Y = 7 };
            
            var priceLabel = new Label("Price:") { X = 1, Y = 9 };
            var priceField = new TextField("") { X = 20, Y = 9, Width = 20 };
            priceField.Enabled = false;
            
            orderTypeRadio.SelectedItemChanged += (args) =>
            {
                priceField.Enabled = orderTypeRadio.SelectedItem == 1; // Enable for Limit orders
                if (orderTypeRadio.SelectedItem == 0)
                {
                    priceField.Text = "";
                }
            };
            
            var btnSend = new Button("Send Order") { X = 1, Y = 11 };
            btnSend.Clicked += () =>
            {
                var symbol = symbolField.Text?.ToString() ?? "";
                var side = sideRadio.SelectedItem == 0 ? "BUY" : "SELL";
                var qtyText = qtyField.Text?.ToString() ?? "";
                var priceText = priceField.Text?.ToString() ?? "";
                
                if (string.IsNullOrWhiteSpace(symbol))
                {
                    MessageBox.ErrorQuery("Error", "Symbol is required", "OK");
                    return;
                }
                
                if (!decimal.TryParse(qtyText, out decimal quantity) || quantity <= 0)
                {
                    MessageBox.ErrorQuery("Error", "Invalid quantity", "OK");
                    return;
                }
                
                decimal? price = null;
                if (orderTypeRadio.SelectedItem == 1) // Limit order
                {
                    if (!decimal.TryParse(priceText, out decimal limitPrice) || limitPrice <= 0)
                    {
                        MessageBox.ErrorQuery("Error", "Invalid price for limit order", "OK");
                        return;
                    }
                    price = limitPrice;
                }
                
                var confirmMsg = $"Send {side} order for {quantity} {symbol}";
                if (price.HasValue)
                    confirmMsg += $" @ {price}";
                else
                    confirmMsg += " (Market)";
                confirmMsg += "?";
                
                var result = MessageBox.Query("Confirm Order", confirmMsg, "Yes", "No");
                if (result == 0)
                {
                    Application.RequestStop();
                    tradingSession?.SendOrder(symbol, side, quantity, price);
                }
            };
            
            var btnCancel = new Button("Cancel") { X = 15, Y = 11 };
            btnCancel.Clicked += () => Application.RequestStop();
            
            dialog.Add(symbolLabel, symbolField, sideLabel, sideRadio, qtyLabel, qtyField,
                       orderTypeLabel, orderTypeRadio, priceLabel, priceField, btnSend, btnCancel);
            Application.Run(dialog);
        }
        
        private static void ShowExecutionReports()
        {
            if (tradingSession == null)
            {
                MessageBox.ErrorQuery("Error", "Trading session not initialized", "OK");
                return;
            }
            
            var reports = tradingSession.ExecutionReports;
            
            var dialog = new Dialog("Execution Reports", 100, 25);
            
            var reportView = new TextView
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 2,
                Height = Dim.Fill() - 4,
                ReadOnly = true
            };
            
            if (reports.Count == 0)
            {
                reportView.Text = "No execution reports received yet.";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("EXECUTION REPORTS");
                sb.AppendLine(new string('═', 95));
                sb.AppendLine($"{"Time",-10} {"OrderID",-15} {"Symbol",-10} {"Side",-5} {"Status",-15} {"Type",-12} {"Qty",8} {"Filled",8} {"Price",10}");
                sb.AppendLine(new string('─', 95));
                
                foreach (var report in reports.OrderByDescending(r => r.Timestamp))
                {
                    var priceStr = report.LastPx.HasValue ? report.LastPx.Value.ToString("F2") : 
                                   (report.Price.HasValue ? report.Price.Value.ToString("F2") : "-");
                    
                    sb.AppendLine($"{report.Timestamp:HH:mm:ss}  {report.OrderID,-15} {report.Symbol,-10} {report.Side,-5} " +
                                  $"{report.OrdStatus,-15} {report.ExecType,-12} {report.OrderQty,8:F0} {report.CumQty,8:F0} {priceStr,10}");
                    
                    if (!string.IsNullOrEmpty(report.Text))
                    {
                        sb.AppendLine($"  └─ {report.Text}");
                    }
                }
                
                sb.AppendLine(new string('═', 95));
                sb.AppendLine($"Total Reports: {reports.Count}");
                
                reportView.Text = sb.ToString();
            }
            
            var btnRefresh = new Button("Refresh") { X = 1, Y = Pos.Bottom(reportView) + 1 };
            btnRefresh.Clicked += () =>
            {
                Application.RequestStop();
                ShowExecutionReports();
            };
            
            var btnClose = new Button("Close") { X = 13, Y = Pos.Bottom(reportView) + 1 };
            btnClose.Clicked += () => Application.RequestStop();
            
            dialog.Add(reportView, btnRefresh, btnClose);
            Application.Run(dialog);
        }
        
        private static void ShowMarketDataFeed()
        {
            var dialog = new Dialog("Market Data Feed", 80, 20);
            
            var infoText = new TextView
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill() - 2,
                Height = Dim.Fill() - 4,
                ReadOnly = true,
                Text = @"Market Data Feed

This feature will allow you to:
  • Subscribe to market data feeds from DSE/CSE
  • View real-time price updates
  • Monitor order book changes
  • Track trade executions

Implementation Status: Placeholder

Market data feed functionality will be implemented in a future update.
It will integrate with the exchange's market data protocols and display
live market information in this window.

Features planned:
  ✓ Symbol subscription
  ✓ Last price updates
  ✓ Bid/Ask prices
  ✓ Order book depth
  ✓ Trade volume
  ✓ Market status
"
            };
            
            var btnClose = new Button("Close") { X = 1, Y = Pos.Bottom(infoText) + 1 };
            btnClose.Clicked += () => Application.RequestStop();
            
            dialog.Add(infoText, btnClose);
            Application.Run(dialog);
            
            AppendOutput("\n[Market Data Feed]\n");
            AppendOutput("Market data feed feature - Coming soon!\n");
        }
        
        private static void ShowSessionLogout()
        {
            if (tradingSession == null || !tradingSession.IsConnected)
            {
                MessageBox.Query("Session Logout", "No active session to disconnect", "OK");
                return;
            }
            
            var exchange = tradingSession.Exchange;
            var result = MessageBox.Query("Confirm Logout", 
                $"Disconnect from {exchange}?", "Yes", "No");
            
            if (result == 0)
            {
                AppendOutput($"\n[Disconnecting from {exchange}]\n");
                tradingSession.Disconnect();
                AppendOutput("Disconnected successfully.\n");
            }
        }
    }
}
