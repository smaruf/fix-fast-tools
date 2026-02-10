using QuickFix;
using QuickFix.Transport;
using QuickFix.Store;
using QuickFix.Logger;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace CommonGUI
{
    public class ExecutionReportData
    {
        public DateTime Timestamp { get; set; }
        public string ExecID { get; set; } = "";
        public string OrderID { get; set; } = "";
        public string ClOrdID { get; set; } = "";
        public string Symbol { get; set; } = "";
        public string Side { get; set; } = "";
        public string ExecType { get; set; } = "";
        public string OrdStatus { get; set; } = "";
        public decimal OrderQty { get; set; }
        public decimal CumQty { get; set; }
        public decimal LeavesQty { get; set; }
        public decimal? Price { get; set; }
        public decimal? LastPx { get; set; }
        public decimal? LastQty { get; set; }
        public string Text { get; set; } = "";
    }

    public class TradingSession
    {
        private IApplication? _fixClient;
        private SocketInitiator? _initiator;
        private SessionSettings? _settings;
        private ILoggerFactory? _loggerFactory;
        private string _exchange = "";
        private bool _isConnected = false;
        private readonly List<ExecutionReportData> _executionReports = new();
        private readonly object _lockObject = new();

        public event Action<string>? OnStatusChanged;
        public event Action<ExecutionReportData>? OnExecutionReport;
        public event Action<string>? OnLogMessage;

        public bool IsConnected => _isConnected;
        public string Exchange => _exchange;
        public List<ExecutionReportData> ExecutionReports
        {
            get
            {
                lock (_lockObject)
                {
                    return new List<ExecutionReportData>(_executionReports);
                }
            }
        }

        public bool Connect(string exchange, string configPath)
        {
            try
            {
                _exchange = exchange;
                
                if (!File.Exists(configPath))
                {
                    OnLogMessage?.Invoke($"Configuration file not found: {configPath}");
                    return false;
                }

                _loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                _settings = new SessionSettings(configPath);
                var storeFactory = new FileStoreFactory(_settings);
                var logFactory = new FileLogFactory(_settings);

                if (exchange.ToUpper() == "DSE")
                {
                    var dseClient = new FixProtocol.DSE.FixClient(_loggerFactory.CreateLogger<FixProtocol.DSE.FixClient>());
                    _fixClient = new FixClientWrapper(dseClient, this);
                    _initiator = new SocketInitiator(_fixClient, storeFactory, _settings, logFactory);
                }
                else if (exchange.ToUpper() == "CSE")
                {
                    var cseClient = new FixProtocol.CSE.FixClient(_loggerFactory.CreateLogger<FixProtocol.CSE.FixClient>());
                    _fixClient = new FixClientWrapper(cseClient, this);
                    _initiator = new SocketInitiator(_fixClient, storeFactory, _settings, logFactory);
                }
                else
                {
                    OnLogMessage?.Invoke($"Unknown exchange: {exchange}");
                    return false;
                }

                _initiator.Start();
                _isConnected = true;
                OnStatusChanged?.Invoke($"Connected to {exchange}");
                OnLogMessage?.Invoke($"Successfully connected to {exchange}");
                
                return true;
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Connection failed: {ex.Message}");
                OnStatusChanged?.Invoke("Disconnected");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_initiator != null)
                {
                    _initiator.Stop();
                    _initiator.Dispose();
                    _initiator = null;
                }

                _fixClient = null;
                _isConnected = false;
                OnStatusChanged?.Invoke("Disconnected");
                OnLogMessage?.Invoke($"Disconnected from {_exchange}");
                _exchange = "";
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Disconnect error: {ex.Message}");
            }
        }

        public void SendOrder(string symbol, string side, decimal quantity, decimal? price = null)
        {
            if (!_isConnected)
            {
                OnLogMessage?.Invoke("Cannot send order - not connected");
                return;
            }

            try
            {
                if (_fixClient is FixClientWrapper wrapper)
                {
                    wrapper.SendOrder(symbol, side, quantity, price);
                    OnLogMessage?.Invoke($"Order sent: {symbol} {side} {quantity}" + (price.HasValue ? $" @ {price}" : " (Market)"));
                }
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error sending order: {ex.Message}");
            }
        }

        internal void AddExecutionReport(ExecutionReportData report)
        {
            lock (_lockObject)
            {
                _executionReports.Add(report);
            }
            OnExecutionReport?.Invoke(report);
        }

        internal void UpdateConnectionStatus(bool connected)
        {
            _isConnected = connected;
            OnStatusChanged?.Invoke(connected ? $"Connected to {_exchange}" : "Disconnected");
        }

        private class FixClientWrapper : IApplication
        {
            private readonly IApplication _innerClient;
            private readonly TradingSession _session;
            private readonly Dictionary<SessionID, Session> _sessions = new();

            public FixClientWrapper(IApplication innerClient, TradingSession session)
            {
                _innerClient = innerClient;
                _session = session;
            }

            public void OnCreate(SessionID sessionID)
            {
                _innerClient.OnCreate(sessionID);
                var session = Session.LookupSession(sessionID);
                if (session != null)
                    _sessions[sessionID] = session;
            }

            public void OnLogon(SessionID sessionID)
            {
                _innerClient.OnLogon(sessionID);
                _session.UpdateConnectionStatus(true);
            }

            public void OnLogout(SessionID sessionID)
            {
                _innerClient.OnLogout(sessionID);
                _session.UpdateConnectionStatus(false);
            }

            public void ToAdmin(Message message, SessionID sessionID)
            {
                _innerClient.ToAdmin(message, sessionID);
            }

            public void FromAdmin(Message message, SessionID sessionID)
            {
                _innerClient.FromAdmin(message, sessionID);
            }

            public void ToApp(Message message, SessionID sessionID)
            {
                _innerClient.ToApp(message, sessionID);
            }

            public void FromApp(Message message, SessionID sessionID)
            {
                _innerClient.FromApp(message, sessionID);
                
                try
                {
                    var msgTypeField = new QuickFix.Fields.MsgType();
                    message.Header.GetField(msgTypeField);
                    
                    if (msgTypeField.Value == "8") // Execution Report
                    {
                        var report = ParseExecutionReport(message);
                        _session.AddExecutionReport(report);
                    }
                }
                catch (Exception)
                {
                    // Ignore parsing errors
                }
            }

            public void SendOrder(string symbol, string side, decimal quantity, decimal? price = null)
            {
                try
                {
                    var order = new QuickFix.FIX44.NewOrderSingle();
                    
                    order.SetField(new QuickFix.Fields.ClOrdID(Guid.NewGuid().ToString()));
                    order.SetField(new QuickFix.Fields.Symbol(symbol));
                    order.SetField(new QuickFix.Fields.Side(side.ToUpper() == "BUY" ? '1' : '2'));
                    order.SetField(new QuickFix.Fields.TransactTime(DateTime.UtcNow));
                    order.SetField(new QuickFix.Fields.OrderQty(quantity));
                    
                    if (price.HasValue)
                    {
                        order.SetField(new QuickFix.Fields.OrdType('2')); // Limit
                        order.SetField(new QuickFix.Fields.Price(price.Value));
                    }
                    else
                    {
                        order.SetField(new QuickFix.Fields.OrdType('1')); // Market
                    }
                    
                    var sessionID = _sessions.Keys.FirstOrDefault();
                    if (sessionID != null && _sessions.TryGetValue(sessionID, out var session))
                    {
                        session.Send(order);
                    }
                }
                catch (Exception ex)
                {
                    _session.OnLogMessage?.Invoke($"Error sending order: {ex.Message}");
                }
            }

            private ExecutionReportData ParseExecutionReport(Message message)
            {
                var report = new ExecutionReportData
                {
                    Timestamp = DateTime.Now
                };

                try
                {
                    if (message.IsSetField(17)) report.ExecID = message.GetString(17);
                    if (message.IsSetField(37)) report.OrderID = message.GetString(37);
                    if (message.IsSetField(11)) report.ClOrdID = message.GetString(11);
                    if (message.IsSetField(55)) report.Symbol = message.GetString(55);
                    if (message.IsSetField(54)) report.Side = message.GetChar(54) == '1' ? "BUY" : "SELL";
                    if (message.IsSetField(150)) report.ExecType = GetExecTypeDescription(message.GetChar(150).ToString());
                    if (message.IsSetField(39)) report.OrdStatus = GetOrderStatusDescription(message.GetChar(39).ToString());
                    if (message.IsSetField(38)) report.OrderQty = message.GetDecimal(38);
                    if (message.IsSetField(14)) report.CumQty = message.GetDecimal(14);
                    if (message.IsSetField(151)) report.LeavesQty = message.GetDecimal(151);
                    if (message.IsSetField(44)) report.Price = message.GetDecimal(44);
                    if (message.IsSetField(31)) report.LastPx = message.GetDecimal(31);
                    if (message.IsSetField(32)) report.LastQty = message.GetDecimal(32);
                    if (message.IsSetField(58)) report.Text = message.GetString(58);
                }
                catch (Exception)
                {
                    // Use defaults for any missing fields
                }

                return report;
            }

            private string GetExecTypeDescription(string execType)
            {
                return execType switch
                {
                    "0" => "New",
                    "1" => "Partial Fill",
                    "2" => "Fill",
                    "4" => "Canceled",
                    "8" => "Rejected",
                    "C" => "Expired",
                    "F" => "Trade",
                    _ => $"Unknown ({execType})"
                };
            }

            private string GetOrderStatusDescription(string ordStatus)
            {
                return ordStatus switch
                {
                    "0" => "New",
                    "1" => "Partially Filled",
                    "2" => "Filled",
                    "4" => "Canceled",
                    "8" => "Rejected",
                    "A" => "Pending New",
                    "C" => "Expired",
                    _ => $"Unknown ({ordStatus})"
                };
            }
        }
    }
}
