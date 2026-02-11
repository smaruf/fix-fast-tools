using Microsoft.AspNetCore.Mvc;
using FastTools.Core.Services;
using FastTools.Core.Models;
using System.Collections.Concurrent;

namespace FastTools.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoadTestController : ControllerBase
    {
        private readonly ILogger<LoadTestController> _logger;
        private static readonly ConcurrentDictionary<string, LoadTestingService> _activeTests = new();

        public LoadTestController(ILogger<LoadTestController> logger)
        {
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<ActionResult> StartLoadTest([FromBody] LoadTestConfig config)
        {
            try
            {
                var testId = Guid.NewGuid().ToString();
                var service = new LoadTestingService(config);

                // Subscribe to progress events
                service.ProgressUpdated += (sender, e) =>
                {
                    _logger.LogInformation("Load test progress: {Sent}/{Total} messages, {Throughput:F2} msg/s",
                        e.MessagesSent, config.Scenario.TotalMessages, e.CurrentThroughput);
                };

                _activeTests[testId] = service;

                // Start test in background (simplified version - just simulate for demo)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await service.RunLoadTestAsync(async (msg) =>
                        {
                            // Simulate message sending with random success/failure
                            await Task.Delay(10);
                            return Random.Shared.Next(100) > 5; // 95% success rate, thread-safe
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Load test {TestId} failed", testId);
                    }
                    finally
                    {
                        // Keep test in dictionary for result retrieval
                    }
                });

                return Accepted(new { testId, message = "Load test started", config.Scenario.TotalMessages });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting load test");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{testId}/status")]
        public ActionResult GetTestStatus(string testId)
        {
            try
            {
                if (!_activeTests.TryGetValue(testId, out var service))
                {
                    return NotFound(new { error = $"Test '{testId}' not found" });
                }

                return Ok(new
                {
                    testId,
                    isRunning = service.IsRunning,
                    metrics = service.Metrics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting test status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{testId}/results")]
        public ActionResult<LoadTestMetrics> GetTestResults(string testId)
        {
            try
            {
                if (!_activeTests.TryGetValue(testId, out var service))
                {
                    return NotFound(new { error = $"Test '{testId}' not found" });
                }

                return Ok(service.Metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting test results");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("active")]
        public ActionResult GetActiveTests()
        {
            try
            {
                var tests = _activeTests.Select(kvp => new
                {
                    testId = kvp.Key,
                    isRunning = kvp.Value.IsRunning,
                    messagesSent = kvp.Value.Metrics.MessagesSent,
                    messagesReceived = kvp.Value.Metrics.MessagesReceived,
                    messagesFailed = kvp.Value.Metrics.MessagesFailed
                }).ToList();

                return Ok(tests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active tests");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("configs/default")]
        public ActionResult<LoadTestConfig> GetDefaultConfig([FromBody] string exchangeCode)
        {
            try
            {
                var config = LoadTestingService.CreateDefaultConfig(exchangeCode);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default config");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("configs/high-throughput")]
        public ActionResult<LoadTestConfig> GetHighThroughputConfig([FromBody] string exchangeCode)
        {
            try
            {
                var config = LoadTestingService.CreateHighThroughputConfig(exchangeCode);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating high-throughput config");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{testId}")]
        public ActionResult CleanupTest(string testId)
        {
            try
            {
                if (_activeTests.TryRemove(testId, out _))
                {
                    _logger.LogInformation("Cleaned up test: {TestId}", testId);
                    return NoContent();
                }

                return NotFound(new { error = $"Test '{testId}' not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up test");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
