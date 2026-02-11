namespace FastTools.Core.Models
{
    public class ExchangeConfig
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string Country { get; set; }
        public string Description { get; set; }
        public ExchangeProtocolConfig Protocol { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class ExchangeProtocolConfig
    {
        public string Type { get; set; } // FIX, ITCH, FAST
        public string Version { get; set; }
        public ConnectionConfig Connection { get; set; }
        public SessionConfig Session { get; set; }
        public Dictionary<string, string> CustomSettings { get; set; }
    }

    public class ConnectionConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public int TimeoutSeconds { get; set; } = 30;
        public int HeartbeatIntervalSeconds { get; set; } = 30;
    }

    public class SessionConfig
    {
        public string SenderCompId { get; set; }
        public string TargetCompId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DataDictionary { get; set; }
        public bool UseDataDictionary { get; set; }
        public string FileStorePath { get; set; }
        public string FileLogPath { get; set; }
    }

    public class ExchangeConfigCollection
    {
        public string Version { get; set; } = "1.0";
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public List<ExchangeConfig> Exchanges { get; set; } = new List<ExchangeConfig>();
    }
}
