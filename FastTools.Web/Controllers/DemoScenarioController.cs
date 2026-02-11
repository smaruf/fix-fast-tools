using Microsoft.AspNetCore.Mvc;
using FastTools.Core.Services;
using FastTools.Core.Models;

namespace FastTools.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DemoScenarioController : ControllerBase
    {
        private readonly ILogger<DemoScenarioController> _logger;

        public DemoScenarioController(ILogger<DemoScenarioController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<List<DemoScenario>> GetAllScenarios()
        {
            try
            {
                var scenarios = DemoScenarioManager.GetAllScenarios();
                return Ok(scenarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting demo scenarios");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public ActionResult<DemoScenario> GetScenarioById(string id)
        {
            try
            {
                var scenario = DemoScenarioManager.GetScenarioById(id);
                
                if (scenario == null)
                {
                    return NotFound(new { error = $"Scenario '{id}' not found" });
                }
                
                return Ok(scenario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting demo scenario");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("category/{category}")]
        public ActionResult<List<DemoScenario>> GetScenariosByCategory(string category)
        {
            try
            {
                var scenarios = DemoScenarioManager.GetScenariosByCategory(category);
                return Ok(scenarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting scenarios by category");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("categories")]
        public ActionResult<List<string>> GetCategories()
        {
            try
            {
                var categories = DemoScenarioManager.GetAllScenarios()
                    .Select(s => s.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
                
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id}/execute")]
        public async Task<ActionResult> ExecuteScenario(string id)
        {
            try
            {
                var scenario = DemoScenarioManager.GetScenarioById(id);
                
                if (scenario == null)
                {
                    return NotFound(new { error = $"Scenario '{id}' not found" });
                }

                var results = new List<object>();

                foreach (var step in scenario.Steps.OrderBy(s => s.Order))
                {
                    _logger.LogInformation("Executing step {Order}: {Title}", step.Order, step.Title);
                    
                    // Simulate step execution
                    await Task.Delay(step.DelayMs);
                    
                    results.Add(new
                    {
                        step = step.Order,
                        title = step.Title,
                        action = step.Action,
                        status = "completed",
                        result = step.ExpectedResult
                    });
                }

                return Ok(new
                {
                    scenarioId = id,
                    scenarioName = scenario.Name,
                    status = "completed",
                    steps = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scenario");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("import")]
        public ActionResult<DemoScenario> ImportScenario([FromBody] string json)
        {
            try
            {
                var scenario = DemoScenarioManager.ImportScenarioFromJson(json);
                return Ok(scenario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing scenario");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}/export")]
        public ActionResult<string> ExportScenario(string id)
        {
            try
            {
                var scenario = DemoScenarioManager.GetScenarioById(id);
                
                if (scenario == null)
                {
                    return NotFound(new { error = $"Scenario '{id}' not found" });
                }

                var json = DemoScenarioManager.ExportScenarioAsJson(scenario);
                return Ok(new { json });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting scenario");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("summary")]
        public ActionResult GetSummary()
        {
            try
            {
                var scenarios = DemoScenarioManager.GetAllScenarios();
                
                var summary = new
                {
                    totalScenarios = scenarios.Count,
                    byCategory = scenarios.GroupBy(s => s.Category)
                        .Select(g => new { category = g.Key, count = g.Count() })
                        .ToList(),
                    byType = scenarios.GroupBy(s => s.Type)
                        .Select(g => new { type = g.Key.ToString(), count = g.Count() })
                        .ToList(),
                    byExchange = scenarios.GroupBy(s => s.ExchangeCode)
                        .Select(g => new { exchange = g.Key, count = g.Count() })
                        .ToList()
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting summary");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
