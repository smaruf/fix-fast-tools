using System.Text.Json;
using System.Text.Json.Serialization;
using FastTools.Core.Models;

namespace FastTools.Core.Services
{
    public class ExchangeConfigManager
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static ExchangeConfigCollection LoadExchangeConfigs(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return CreateDefaultConfigs();
            }

            try
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<ExchangeConfigCollection>(json, _jsonOptions);
                return config ?? CreateDefaultConfigs();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config from {configPath}: {ex.Message}");
                return CreateDefaultConfigs();
            }
        }

        public static void SaveExchangeConfigs(string configPath, ExchangeConfigCollection configs)
        {
            try
            {
                configs.LastModified = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(configs, _jsonOptions);
                
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config to {configPath}: {ex.Message}");
                throw;
            }
        }

        public static ExchangeConfig GetExchangeByCode(ExchangeConfigCollection configs, string code)
        {
            return configs.Exchanges.FirstOrDefault(e => 
                e.Code.Equals(code, StringComparison.OrdinalIgnoreCase) && e.IsEnabled);
        }

        public static List<ExchangeConfig> GetExchangesByProtocol(ExchangeConfigCollection configs, string protocolType)
        {
            return configs.Exchanges
                .Where(e => e.IsEnabled && 
                            e.Protocol?.Type.Equals(protocolType, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        public static bool ValidateConfig(ExchangeConfig config, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(config.Name))
                errors.Add("Exchange name is required");

            if (string.IsNullOrWhiteSpace(config.Code))
                errors.Add("Exchange code is required");

            if (config.Protocol == null)
            {
                errors.Add("Protocol configuration is required");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(config.Protocol.Type))
                    errors.Add("Protocol type is required (FIX, ITCH, or FAST)");

                if (config.Protocol.Connection == null)
                {
                    errors.Add("Connection configuration is required");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(config.Protocol.Connection.Host))
                        errors.Add("Connection host is required");

                    if (config.Protocol.Connection.Port <= 0 || config.Protocol.Connection.Port > 65535)
                        errors.Add("Connection port must be between 1 and 65535");
                }

                if (config.Protocol.Type?.Equals("FIX", StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (config.Protocol.Session == null)
                    {
                        errors.Add("FIX protocol requires session configuration");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(config.Protocol.Session.SenderCompId))
                            errors.Add("FIX SenderCompId is required");

                        if (string.IsNullOrWhiteSpace(config.Protocol.Session.TargetCompId))
                            errors.Add("FIX TargetCompId is required");
                    }
                }
            }

            return errors.Count == 0;
        }

        public static ExchangeConfigCollection CreateDefaultConfigs()
        {
            var configs = new ExchangeConfigCollection
            {
                Version = "1.0",
                LastModified = DateTime.UtcNow,
                Exchanges = new List<ExchangeConfig>
                {
                    new ExchangeConfig
                    {
                        Name = "Dhaka Stock Exchange",
                        Code = "DSE",
                        Country = "Bangladesh",
                        Description = "DSE-BD - Dhaka Stock Exchange, Bangladesh",
                        IsEnabled = true,
                        Protocol = new ExchangeProtocolConfig
                        {
                            Type = "FIX",
                            Version = "4.4",
                            Connection = new ConnectionConfig
                            {
                                Host = "localhost",
                                Port = 5001,
                                UseSsl = false,
                                TimeoutSeconds = 30,
                                HeartbeatIntervalSeconds = 30
                            },
                            Session = new SessionConfig
                            {
                                SenderCompId = "CLIENT1",
                                TargetCompId = "DSE-BD",
                                FileStorePath = "./data/dse",
                                FileLogPath = "./logs/dse",
                                UseDataDictionary = false
                            }
                        }
                    },
                    new ExchangeConfig
                    {
                        Name = "Chittagong Stock Exchange",
                        Code = "CSE",
                        Country = "Bangladesh",
                        Description = "CSE-BD - Chittagong Stock Exchange, Bangladesh",
                        IsEnabled = true,
                        Protocol = new ExchangeProtocolConfig
                        {
                            Type = "FIX",
                            Version = "4.4",
                            Connection = new ConnectionConfig
                            {
                                Host = "localhost",
                                Port = 5002,
                                UseSsl = false,
                                TimeoutSeconds = 30,
                                HeartbeatIntervalSeconds = 30
                            },
                            Session = new SessionConfig
                            {
                                SenderCompId = "CLIENT1",
                                TargetCompId = "CSE-BD",
                                FileStorePath = "./data/cse",
                                FileLogPath = "./logs/cse",
                                UseDataDictionary = false
                            }
                        }
                    },
                    new ExchangeConfig
                    {
                        Name = "DSE Market Data (ITCH)",
                        Code = "DSE-ITCH",
                        Country = "Bangladesh",
                        Description = "DSE-BD Market Data via NASDAQ ITCH 5.0 Protocol",
                        IsEnabled = true,
                        Protocol = new ExchangeProtocolConfig
                        {
                            Type = "ITCH",
                            Version = "5.0",
                            Connection = new ConnectionConfig
                            {
                                Host = "localhost",
                                Port = 6001,
                                UseSsl = false,
                                TimeoutSeconds = 30
                            },
                            CustomSettings = new Dictionary<string, string>
                            {
                                { "MulticastGroup", "239.0.0.1" },
                                { "NetworkInterface", "0.0.0.0" }
                            }
                        }
                    },
                    new ExchangeConfig
                    {
                        Name = "DSE Market Data (FAST)",
                        Code = "DSE-FAST",
                        Country = "Bangladesh",
                        Description = "DSE-BD Market Data via FAST Protocol",
                        IsEnabled = true,
                        Protocol = new ExchangeProtocolConfig
                        {
                            Type = "FAST",
                            Version = "1.1",
                            Connection = new ConnectionConfig
                            {
                                Host = "localhost",
                                Port = 6002,
                                UseSsl = false,
                                TimeoutSeconds = 30
                            },
                            CustomSettings = new Dictionary<string, string>
                            {
                                { "TemplateFile", "FAST_TEMPLATE.xml" },
                                { "ResetOnEveryMessage", "false" }
                            }
                        }
                    },
                    new ExchangeConfig
                    {
                        Name = "Sample Test Exchange",
                        Code = "TEST",
                        Country = "Global",
                        Description = "Sample test exchange for demonstration and learning",
                        IsEnabled = true,
                        Protocol = new ExchangeProtocolConfig
                        {
                            Type = "FIX",
                            Version = "4.4",
                            Connection = new ConnectionConfig
                            {
                                Host = "localhost",
                                Port = 5999,
                                UseSsl = false,
                                TimeoutSeconds = 30,
                                HeartbeatIntervalSeconds = 30
                            },
                            Session = new SessionConfig
                            {
                                SenderCompId = "TESTCLIENT",
                                TargetCompId = "TESTSERVER",
                                FileStorePath = "./data/test",
                                FileLogPath = "./logs/test",
                                UseDataDictionary = false
                            }
                        }
                    }
                }
            };

            return configs;
        }

        public static string ExportConfigAsJson(ExchangeConfig config)
        {
            return JsonSerializer.Serialize(config, _jsonOptions);
        }

        public static ExchangeConfig ImportConfigFromJson(string json)
        {
            return JsonSerializer.Deserialize<ExchangeConfig>(json, _jsonOptions);
        }
    }
}
