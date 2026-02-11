using Microsoft.AspNetCore.Mvc;
using FastTools.Core.Services;
using FastTools.Core.Models;

namespace FastTools.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeConfigController : ControllerBase
    {
        private readonly ILogger<ExchangeConfigController> _logger;
        private readonly string _configPath;

        public ExchangeConfigController(ILogger<ExchangeConfigController> logger)
        {
            _logger = logger;
            _configPath = Path.Combine(Directory.GetCurrentDirectory(), "configs", "exchanges.json");
        }

        [HttpGet]
        public ActionResult<ExchangeConfigCollection> GetAllConfigs()
        {
            try
            {
                var configs = ExchangeConfigManager.LoadExchangeConfigs(_configPath);
                return Ok(configs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading exchange configs");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{code}")]
        public ActionResult<ExchangeConfig> GetConfigByCode(string code)
        {
            try
            {
                var configs = ExchangeConfigManager.LoadExchangeConfigs(_configPath);
                var exchange = ExchangeConfigManager.GetExchangeByCode(configs, code);
                
                if (exchange == null)
                {
                    return NotFound(new { error = $"Exchange '{code}' not found" });
                }
                
                return Ok(exchange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchange config");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("protocol/{protocolType}")]
        public ActionResult<List<ExchangeConfig>> GetConfigsByProtocol(string protocolType)
        {
            try
            {
                var configs = ExchangeConfigManager.LoadExchangeConfigs(_configPath);
                var exchanges = ExchangeConfigManager.GetExchangesByProtocol(configs, protocolType);
                return Ok(exchanges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exchanges by protocol");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult<ExchangeConfig> AddConfig([FromBody] ExchangeConfig config)
        {
            try
            {
                if (!ExchangeConfigManager.ValidateConfig(config, out var errors))
                {
                    return BadRequest(new { errors });
                }

                var configs = ExchangeConfigManager.LoadExchangeConfigs(_configPath);
                
                // Check for duplicate code
                if (configs.Exchanges.Any(e => e.Code.Equals(config.Code, StringComparison.OrdinalIgnoreCase)))
                {
                    return Conflict(new { error = $"Exchange with code '{config.Code}' already exists" });
                }

                configs.Exchanges.Add(config);
                ExchangeConfigManager.SaveExchangeConfigs(_configPath, configs);
                
                _logger.LogInformation("Added new exchange config: {Code}", config.Code);
                return CreatedAtAction(nameof(GetConfigByCode), new { code = config.Code }, config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding exchange config");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{code}")]
        public ActionResult<ExchangeConfig> UpdateConfig(string code, [FromBody] ExchangeConfig config)
        {
            try
            {
                if (!ExchangeConfigManager.ValidateConfig(config, out var errors))
                {
                    return BadRequest(new { errors });
                }

                var configs = ExchangeConfigManager.LoadExchangeConfigs(_configPath);
                var existing = configs.Exchanges.FirstOrDefault(e => 
                    e.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
                
                if (existing == null)
                {
                    return NotFound(new { error = $"Exchange '{code}' not found" });
                }

                var index = configs.Exchanges.IndexOf(existing);
                configs.Exchanges[index] = config;
                ExchangeConfigManager.SaveExchangeConfigs(_configPath, configs);
                
                _logger.LogInformation("Updated exchange config: {Code}", config.Code);
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exchange config");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("{code}")]
        public ActionResult DeleteConfig(string code)
        {
            try
            {
                var configs = ExchangeConfigManager.LoadExchangeConfigs(_configPath);
                var existing = configs.Exchanges.FirstOrDefault(e => 
                    e.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
                
                if (existing == null)
                {
                    return NotFound(new { error = $"Exchange '{code}' not found" });
                }

                configs.Exchanges.Remove(existing);
                ExchangeConfigManager.SaveExchangeConfigs(_configPath, configs);
                
                _logger.LogInformation("Deleted exchange config: {Code}", code);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exchange config");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{code}/validate")]
        public ActionResult ValidateExchangeConfig(string code)
        {
            try
            {
                var configs = ExchangeConfigManager.LoadExchangeConfigs(_configPath);
                var exchange = ExchangeConfigManager.GetExchangeByCode(configs, code);
                
                if (exchange == null)
                {
                    return NotFound(new { error = $"Exchange '{code}' not found" });
                }

                if (!ExchangeConfigManager.ValidateConfig(exchange, out var errors))
                {
                    return Ok(new { valid = false, errors });
                }

                return Ok(new { valid = true, message = "Configuration is valid" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating exchange config");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("import")]
        public ActionResult<ExchangeConfig> ImportConfig([FromBody] string json)
        {
            try
            {
                var config = ExchangeConfigManager.ImportConfigFromJson(json);
                
                if (!ExchangeConfigManager.ValidateConfig(config, out var errors))
                {
                    return BadRequest(new { errors });
                }

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing exchange config");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{code}/export")]
        public ActionResult<string> ExportConfig(string code)
        {
            try
            {
                var configs = ExchangeConfigManager.LoadExchangeConfigs(_configPath);
                var exchange = ExchangeConfigManager.GetExchangeByCode(configs, code);
                
                if (exchange == null)
                {
                    return NotFound(new { error = $"Exchange '{code}' not found" });
                }

                var json = ExchangeConfigManager.ExportConfigAsJson(exchange);
                return Ok(new { json });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting exchange config");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
