using System.Diagnostics;
using FastTools.Core.Models;

namespace FastTools.Core.Services
{
    public class LoadTestingService
    {
        private readonly LoadTestConfig _config;
        private readonly LoadTestMetrics _metrics;
        private readonly List<LatencyMeasurement> _latencies;
        private bool _isRunning;

        public bool IsRunning => _isRunning;
        public LoadTestMetrics Metrics => _metrics;

        public LoadTestingService(LoadTestConfig config)
        {
            _config = config;
            _metrics = config.Metrics;
            _latencies = new List<LatencyMeasurement>();
        }

        public event EventHandler<LoadTestProgressEventArgs> ProgressUpdated;
        public event EventHandler<LoadTestCompletedEventArgs> TestCompleted;

        public async Task<LoadTestMetrics> RunLoadTestAsync(
            Func<string, Task<bool>> sendMessageFunc,
            CancellationToken cancellationToken = default)
        {
            _isRunning = true;
            _metrics.StartTime = DateTime.UtcNow;
            _metrics.MessagesSent = 0;
            _metrics.MessagesReceived = 0;
            _metrics.MessagesFailed = 0;
            _latencies.Clear();

            var scenario = _config.Scenario;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (scenario.RampUp)
                {
                    await RunWithRampUp(sendMessageFunc, scenario, cancellationToken);
                }
                else
                {
                    await RunConstantLoad(sendMessageFunc, scenario, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Load test cancelled by user");
            }
            finally
            {
                stopwatch.Stop();
                _metrics.EndTime = DateTime.UtcNow;
                _isRunning = false;

                CalculateFinalMetrics();
                OnTestCompleted(new LoadTestCompletedEventArgs { Metrics = _metrics });
            }

            return _metrics;
        }

        private async Task RunWithRampUp(
            Func<string, Task<bool>> sendMessageFunc,
            LoadTestScenario scenario,
            CancellationToken cancellationToken)
        {
            var rampUpInterval = scenario.RampUpSeconds * 1000 / scenario.MessagesPerSecond;
            var targetInterval = 1000.0 / scenario.MessagesPerSecond;

            for (int i = 0; i < scenario.TotalMessages && !cancellationToken.IsCancellationRequested; i++)
            {
                var currentInterval = rampUpInterval - ((rampUpInterval - targetInterval) * i / scenario.TotalMessages);
                
                await SendSingleMessage(sendMessageFunc, i);
                
                if (i < scenario.TotalMessages - 1)
                {
                    await Task.Delay((int)currentInterval, cancellationToken);
                }
            }
        }

        private async Task RunConstantLoad(
            Func<string, Task<bool>> sendMessageFunc,
            LoadTestScenario scenario,
            CancellationToken cancellationToken)
        {
            var intervalMs = 1000.0 / scenario.MessagesPerSecond;
            var nextSendTime = DateTime.UtcNow;

            for (int i = 0; i < scenario.TotalMessages && !cancellationToken.IsCancellationRequested; i++)
            {
                await SendSingleMessage(sendMessageFunc, i);

                nextSendTime = nextSendTime.AddMilliseconds(intervalMs);
                var delay = (int)(nextSendTime - DateTime.UtcNow).TotalMilliseconds;
                
                if (delay > 0)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        private async Task SendSingleMessage(Func<string, Task<bool>> sendMessageFunc, int index)
        {
            var messageType = DetermineMessageType(index);
            var message = GenerateTestMessage(messageType, index);

            var sw = Stopwatch.StartNew();
            bool success = false;

            try
            {
                success = await sendMessageFunc(message);
                sw.Stop();

                if (success)
                {
                    _metrics.MessagesSent++;
                    _metrics.MessagesReceived++;
                    
                    var latency = new LatencyMeasurement
                    {
                        Timestamp = DateTime.UtcNow,
                        LatencyMs = sw.Elapsed.TotalMilliseconds,
                        MessageType = messageType
                    };
                    _latencies.Add(latency);
                    _metrics.Latencies.Add(latency);
                }
                else
                {
                    _metrics.MessagesSent++;
                    _metrics.MessagesFailed++;
                }
            }
            catch (Exception ex)
            {
                _metrics.MessagesSent++;
                _metrics.MessagesFailed++;
                Console.WriteLine($"Error sending message {index}: {ex.Message}");
            }

            OnProgressUpdated(new LoadTestProgressEventArgs
            {
                MessagesSent = _metrics.MessagesSent,
                MessagesReceived = _metrics.MessagesReceived,
                MessagesFailed = _metrics.MessagesFailed,
                CurrentThroughput = CalculateCurrentThroughput()
            });
        }

        private string DetermineMessageType(int index)
        {
            var distribution = _config.Scenario.Distribution;
            var random = Random.Shared; // Use shared Random for thread-safety
            var value = random.Next(100);

            if (value < distribution.NewOrderPercent)
                return "NewOrder";
            else if (value < distribution.NewOrderPercent + distribution.CancelPercent)
                return "Cancel";
            else if (value < distribution.NewOrderPercent + distribution.CancelPercent + distribution.ReplacePercent)
                return "Replace";
            else
                return "Status";
        }

        private string GenerateTestMessage(string messageType, int index)
        {
            return $"{messageType}_{index}_{DateTime.UtcNow:HHmmss}";
        }

        private void CalculateFinalMetrics()
        {
            if (_latencies.Count > 0)
            {
                _metrics.AverageLatencyMs = _latencies.Average(l => l.LatencyMs);
                _metrics.MinLatencyMs = _latencies.Min(l => l.LatencyMs);
                _metrics.MaxLatencyMs = _latencies.Max(l => l.LatencyMs);
            }

            var duration = (_metrics.EndTime - _metrics.StartTime).TotalSeconds;
            _metrics.ThroughputMps = duration > 0 ? _metrics.MessagesReceived / duration : 0;
        }

        private double CalculateCurrentThroughput()
        {
            var elapsed = (DateTime.UtcNow - _metrics.StartTime).TotalSeconds;
            return elapsed > 0 ? _metrics.MessagesSent / elapsed : 0;
        }

        protected virtual void OnProgressUpdated(LoadTestProgressEventArgs e)
        {
            ProgressUpdated?.Invoke(this, e);
        }

        protected virtual void OnTestCompleted(LoadTestCompletedEventArgs e)
        {
            TestCompleted?.Invoke(this, e);
        }

        public static LoadTestConfig CreateDefaultConfig(string exchangeCode)
        {
            return new LoadTestConfig
            {
                Name = $"Default Load Test - {exchangeCode}",
                Description = "Standard load test with moderate throughput",
                ExchangeCode = exchangeCode,
                Scenario = new LoadTestScenario
                {
                    DurationSeconds = 60,
                    MessagesPerSecond = 10,
                    TotalMessages = 600,
                    RampUp = true,
                    RampUpSeconds = 10,
                    Distribution = new MessageDistribution
                    {
                        NewOrderPercent = 60,
                        CancelPercent = 20,
                        ReplacePercent = 15,
                        StatusPercent = 5
                    }
                }
            };
        }

        public static LoadTestConfig CreateHighThroughputConfig(string exchangeCode)
        {
            return new LoadTestConfig
            {
                Name = $"High Throughput Test - {exchangeCode}",
                Description = "Stress test with high message rate",
                ExchangeCode = exchangeCode,
                Scenario = new LoadTestScenario
                {
                    DurationSeconds = 120,
                    MessagesPerSecond = 100,
                    TotalMessages = 12000,
                    RampUp = true,
                    RampUpSeconds = 20,
                    Distribution = new MessageDistribution
                    {
                        NewOrderPercent = 70,
                        CancelPercent = 15,
                        ReplacePercent = 10,
                        StatusPercent = 5
                    }
                }
            };
        }
    }

    public class LoadTestProgressEventArgs : EventArgs
    {
        public int MessagesSent { get; set; }
        public int MessagesReceived { get; set; }
        public int MessagesFailed { get; set; }
        public double CurrentThroughput { get; set; }
    }

    public class LoadTestCompletedEventArgs : EventArgs
    {
        public LoadTestMetrics Metrics { get; set; }
    }
}
