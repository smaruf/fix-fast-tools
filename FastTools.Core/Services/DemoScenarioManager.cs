using System.Text.Json;
using FastTools.Core.Models;

namespace FastTools.Core.Services
{
    public class DemoScenarioManager
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static List<DemoScenario> GetAllScenarios()
        {
            return new List<DemoScenario>
            {
                CreateBasicOrderScenario(),
                CreateMarketDataScenario(),
                CreateSessionManagementScenario(),
                CreateCancelReplaceScenario(),
                CreateErrorHandlingScenario(),
                CreatePerformanceTestScenario()
            };
        }

        public static List<DemoScenario> GetScenariosByCategory(string category)
        {
            return GetAllScenarios()
                .Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public static DemoScenario GetScenarioById(string id)
        {
            return GetAllScenarios().FirstOrDefault(s => s.Id == id);
        }

        private static DemoScenario CreateBasicOrderScenario()
        {
            return new DemoScenario
            {
                Id = "basic-order-001",
                Name = "Basic Order Placement",
                Description = "Learn how to place a simple buy order on the stock exchange",
                Category = "Basic",
                Type = DemoScenarioType.OrderPlacement,
                ExchangeCode = "DSE",
                Steps = new List<DemoStep>
                {
                    new DemoStep
                    {
                        Order = 1,
                        Title = "Connect to Exchange",
                        Description = "Establish connection to DSE-BD FIX server",
                        Action = "CONNECT",
                        Parameters = new Dictionary<string, object>
                        {
                            { "exchangeCode", "DSE" },
                            { "mode", "client" }
                        },
                        ExpectedResult = "Connection established successfully",
                        AutoExecute = true,
                        DelayMs = 2000
                    },
                    new DemoStep
                    {
                        Order = 2,
                        Title = "Send Test Order",
                        Description = "Place a buy order for 100 shares of ACI at market price",
                        Action = "SEND_ORDER",
                        Parameters = new Dictionary<string, object>
                        {
                            { "symbol", "ACI" },
                            { "side", "BUY" },
                            { "quantity", 100 },
                            { "orderType", "MARKET" }
                        },
                        ExpectedResult = "Order accepted, execution report received",
                        AutoExecute = false,
                        DelayMs = 1000
                    },
                    new DemoStep
                    {
                        Order = 3,
                        Title = "View Execution Report",
                        Description = "Check the execution report for order confirmation",
                        Action = "VIEW_REPORT",
                        Parameters = new Dictionary<string, object>(),
                        ExpectedResult = "Execution report shows order filled",
                        AutoExecute = true,
                        DelayMs = 1000
                    },
                    new DemoStep
                    {
                        Order = 4,
                        Title = "Disconnect",
                        Description = "Gracefully disconnect from the exchange",
                        Action = "DISCONNECT",
                        Parameters = new Dictionary<string, object>(),
                        ExpectedResult = "Session terminated successfully",
                        AutoExecute = true,
                        DelayMs = 1000
                    }
                }
            };
        }

        private static DemoScenario CreateMarketDataScenario()
        {
            return new DemoScenario
            {
                Id = "market-data-001",
                Name = "Market Data Consumption",
                Description = "Learn how to receive and process real-time market data via ITCH protocol",
                Category = "Basic",
                Type = DemoScenarioType.MarketData,
                ExchangeCode = "DSE-ITCH",
                Steps = new List<DemoStep>
                {
                    new DemoStep
                    {
                        Order = 1,
                        Title = "Start ITCH Consumer",
                        Description = "Initialize ITCH market data consumer",
                        Action = "START_ITCH",
                        Parameters = new Dictionary<string, object>
                        {
                            { "exchangeCode", "DSE-ITCH" }
                        },
                        ExpectedResult = "ITCH consumer started successfully",
                        AutoExecute = true,
                        DelayMs = 1000
                    },
                    new DemoStep
                    {
                        Order = 2,
                        Title = "Process Sample Messages",
                        Description = "Process 10 sample ITCH messages",
                        Action = "PROCESS_SAMPLE",
                        Parameters = new Dictionary<string, object>
                        {
                            { "messageCount", 10 }
                        },
                        ExpectedResult = "All messages processed successfully",
                        AutoExecute = true,
                        DelayMs = 3000
                    },
                    new DemoStep
                    {
                        Order = 3,
                        Title = "View Statistics",
                        Description = "Display market data statistics",
                        Action = "VIEW_STATS",
                        Parameters = new Dictionary<string, object>(),
                        ExpectedResult = "Statistics displayed",
                        AutoExecute = true,
                        DelayMs = 1000
                    }
                }
            };
        }

        private static DemoScenario CreateSessionManagementScenario()
        {
            return new DemoScenario
            {
                Id = "session-mgmt-001",
                Name = "FIX Session Management",
                Description = "Understand FIX session lifecycle including logon, heartbeats, and logout",
                Category = "Intermediate",
                Type = DemoScenarioType.SessionManagement,
                ExchangeCode = "DSE",
                Steps = new List<DemoStep>
                {
                    new DemoStep
                    {
                        Order = 1,
                        Title = "Initiate Logon",
                        Description = "Send FIX logon message to establish session",
                        Action = "FIX_LOGON",
                        Parameters = new Dictionary<string, object>
                        {
                            { "senderCompId", "CLIENT1" },
                            { "targetCompId", "DSE-BD" }
                        },
                        ExpectedResult = "Logon accepted, session established",
                        AutoExecute = true,
                        DelayMs = 2000
                    },
                    new DemoStep
                    {
                        Order = 2,
                        Title = "Monitor Heartbeats",
                        Description = "Observe heartbeat messages (30 second interval)",
                        Action = "MONITOR_HEARTBEAT",
                        Parameters = new Dictionary<string, object>
                        {
                            { "duration", 10 }
                        },
                        ExpectedResult = "Heartbeats exchanged successfully",
                        AutoExecute = true,
                        DelayMs = 10000
                    },
                    new DemoStep
                    {
                        Order = 3,
                        Title = "Send Logout",
                        Description = "Gracefully terminate FIX session",
                        Action = "FIX_LOGOUT",
                        Parameters = new Dictionary<string, object>(),
                        ExpectedResult = "Logout confirmed, session closed",
                        AutoExecute = true,
                        DelayMs = 2000
                    }
                }
            };
        }

        private static DemoScenario CreateCancelReplaceScenario()
        {
            return new DemoScenario
            {
                Id = "cancel-replace-001",
                Name = "Order Cancel and Replace",
                Description = "Learn how to modify existing orders using cancel and replace requests",
                Category = "Intermediate",
                Type = DemoScenarioType.OrderPlacement,
                ExchangeCode = "DSE",
                Steps = new List<DemoStep>
                {
                    new DemoStep
                    {
                        Order = 1,
                        Title = "Place Original Order",
                        Description = "Place limit order for 100 shares at 850.00",
                        Action = "SEND_ORDER",
                        Parameters = new Dictionary<string, object>
                        {
                            { "symbol", "ACI" },
                            { "side", "BUY" },
                            { "quantity", 100 },
                            { "price", 850.00 },
                            { "orderType", "LIMIT" }
                        },
                        ExpectedResult = "Order placed successfully",
                        AutoExecute = true,
                        DelayMs = 2000
                    },
                    new DemoStep
                    {
                        Order = 2,
                        Title = "Cancel-Replace Order",
                        Description = "Modify order to 150 shares at 860.00",
                        Action = "REPLACE_ORDER",
                        Parameters = new Dictionary<string, object>
                        {
                            { "newQuantity", 150 },
                            { "newPrice", 860.00 }
                        },
                        ExpectedResult = "Order replaced successfully",
                        AutoExecute = false,
                        DelayMs = 2000
                    },
                    new DemoStep
                    {
                        Order = 3,
                        Title = "Cancel Order",
                        Description = "Cancel the modified order",
                        Action = "CANCEL_ORDER",
                        Parameters = new Dictionary<string, object>(),
                        ExpectedResult = "Order cancelled successfully",
                        AutoExecute = false,
                        DelayMs = 1000
                    }
                }
            };
        }

        private static DemoScenario CreateErrorHandlingScenario()
        {
            return new DemoScenario
            {
                Id = "error-handling-001",
                Name = "Error Handling and Recovery",
                Description = "Learn how to handle common errors and recover from failures",
                Category = "Advanced",
                Type = DemoScenarioType.ErrorHandling,
                ExchangeCode = "DSE",
                Steps = new List<DemoStep>
                {
                    new DemoStep
                    {
                        Order = 1,
                        Title = "Send Invalid Order",
                        Description = "Attempt to send order with invalid symbol",
                        Action = "SEND_ORDER",
                        Parameters = new Dictionary<string, object>
                        {
                            { "symbol", "INVALID" },
                            { "side", "BUY" },
                            { "quantity", 100 }
                        },
                        ExpectedResult = "Order rejected with error message",
                        AutoExecute = true,
                        DelayMs = 2000
                    },
                    new DemoStep
                    {
                        Order = 2,
                        Title = "Handle Rejection",
                        Description = "Process rejection message and log error",
                        Action = "HANDLE_REJECTION",
                        Parameters = new Dictionary<string, object>(),
                        ExpectedResult = "Rejection handled gracefully",
                        AutoExecute = true,
                        DelayMs = 1000
                    },
                    new DemoStep
                    {
                        Order = 3,
                        Title = "Retry with Valid Symbol",
                        Description = "Resend order with correct symbol",
                        Action = "SEND_ORDER",
                        Parameters = new Dictionary<string, object>
                        {
                            { "symbol", "ACI" },
                            { "side", "BUY" },
                            { "quantity", 100 }
                        },
                        ExpectedResult = "Order accepted successfully",
                        AutoExecute = true,
                        DelayMs = 2000
                    }
                }
            };
        }

        private static DemoScenario CreatePerformanceTestScenario()
        {
            return new DemoScenario
            {
                Id = "perf-test-001",
                Name = "Performance Testing",
                Description = "Run a basic performance test to measure throughput and latency",
                Category = "Advanced",
                Type = DemoScenarioType.PerformanceTest,
                ExchangeCode = "DSE",
                Steps = new List<DemoStep>
                {
                    new DemoStep
                    {
                        Order = 1,
                        Title = "Configure Load Test",
                        Description = "Set up load test parameters (100 messages, 10 msg/sec)",
                        Action = "CONFIGURE_LOAD_TEST",
                        Parameters = new Dictionary<string, object>
                        {
                            { "totalMessages", 100 },
                            { "messagesPerSecond", 10 },
                            { "rampUp", true }
                        },
                        ExpectedResult = "Load test configured",
                        AutoExecute = true,
                        DelayMs = 1000
                    },
                    new DemoStep
                    {
                        Order = 2,
                        Title = "Execute Load Test",
                        Description = "Run the configured load test",
                        Action = "RUN_LOAD_TEST",
                        Parameters = new Dictionary<string, object>(),
                        ExpectedResult = "Load test completed successfully",
                        AutoExecute = false,
                        DelayMs = 15000
                    },
                    new DemoStep
                    {
                        Order = 3,
                        Title = "View Results",
                        Description = "Display performance metrics and statistics",
                        Action = "VIEW_METRICS",
                        Parameters = new Dictionary<string, object>(),
                        ExpectedResult = "Metrics displayed",
                        AutoExecute = true,
                        DelayMs = 1000
                    }
                }
            };
        }

        public static string ExportScenarioAsJson(DemoScenario scenario)
        {
            return JsonSerializer.Serialize(scenario, _jsonOptions);
        }

        public static DemoScenario ImportScenarioFromJson(string json)
        {
            return JsonSerializer.Deserialize<DemoScenario>(json, _jsonOptions);
        }

        public static void SaveScenario(string path, DemoScenario scenario)
        {
            var json = ExportScenarioAsJson(scenario);
            File.WriteAllText(path, json);
        }

        public static DemoScenario LoadScenario(string path)
        {
            var json = File.ReadAllText(path);
            return ImportScenarioFromJson(json);
        }
    }
}
