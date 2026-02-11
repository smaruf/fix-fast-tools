namespace FastTools.Core.Models
{
    public class LoadTestConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ExchangeCode { get; set; }
        public LoadTestScenario Scenario { get; set; }
        public LoadTestMetrics Metrics { get; set; } = new LoadTestMetrics();
    }

    public class LoadTestScenario
    {
        public int DurationSeconds { get; set; } = 60;
        public int MessagesPerSecond { get; set; } = 10;
        public int TotalMessages { get; set; } = 100;
        public MessageDistribution Distribution { get; set; }
        public bool RampUp { get; set; }
        public int RampUpSeconds { get; set; } = 10;
    }

    public class MessageDistribution
    {
        public int NewOrderPercent { get; set; } = 60;
        public int CancelPercent { get; set; } = 20;
        public int ReplacePercent { get; set; } = 15;
        public int StatusPercent { get; set; } = 5;
    }

    public class LoadTestMetrics
    {
        public int MessagesSent { get; set; }
        public int MessagesReceived { get; set; }
        public int MessagesFailed { get; set; }
        public double AverageLatencyMs { get; set; }
        public double MinLatencyMs { get; set; }
        public double MaxLatencyMs { get; set; }
        public double ThroughputMps { get; set; } // Messages per second
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<LatencyMeasurement> Latencies { get; set; } = new List<LatencyMeasurement>();
    }

    public class LatencyMeasurement
    {
        public DateTime Timestamp { get; set; }
        public double LatencyMs { get; set; }
        public string MessageType { get; set; }
    }
}
