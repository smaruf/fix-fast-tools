namespace FastTools.Core.Models
{
    public class DemoScenario
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; } // Basic, Intermediate, Advanced
        public DemoScenarioType Type { get; set; }
        public List<DemoStep> Steps { get; set; } = new List<DemoStep>();
        public string ExchangeCode { get; set; }
    }

    public enum DemoScenarioType
    {
        OrderPlacement,
        MarketData,
        SessionManagement,
        ErrorHandling,
        PerformanceTest
    }

    public class DemoStep
    {
        public int Order { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string ExpectedResult { get; set; }
        public bool AutoExecute { get; set; }
        public int DelayMs { get; set; }
    }
}
