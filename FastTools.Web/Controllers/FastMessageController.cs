using Microsoft.AspNetCore.Mvc;
using FastTools.Core.Services;
using FastTools.Core.Models;

namespace FastTools.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FastMessageController : ControllerBase
    {
        private readonly FastMessageDecoder _decoder;
        private readonly ILogger<FastMessageController> _logger;

        public FastMessageController(ILogger<FastMessageController> logger)
        {
            _logger = logger;
            _decoder = new FastMessageDecoder();
            
            // Try to load template if available
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "FAST_TEMPLATE.xml");
            if (System.IO.File.Exists(templatePath))
            {
                _decoder.LoadTemplateMap(templatePath);
            }
        }

        [HttpPost("decode/base64")]
        public ActionResult<DecodedMessage> DecodeBase64([FromBody] Base64Request request)
        {
            try
            {
                var bytes = Convert.FromBase64String(request.Data);
                var result = _decoder.DecodeBinary(bytes, request.TemplateId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding base64 message");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("decode/hex")]
        public ActionResult<DecodedMessage> DecodeHex([FromBody] HexRequest request)
        {
            try
            {
                var hex = request.Data.Replace(" ", "").Replace("-", "");
                
                // Validate hex string length is even
                if (hex.Length % 2 != 0)
                {
                    return BadRequest(new { error = "Hex string must have an even number of characters" });
                }
                
                var bytes = new List<byte>();
                for (int i = 0; i < hex.Length; i += 2)
                {
                    bytes.Add(Convert.ToByte(hex.Substring(i, 2), 16));
                }
                var result = _decoder.DecodeBinary(bytes.ToArray(), request.TemplateId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding hex message");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("decode/file")]
        public async Task<ActionResult<DecodedMessage>> DecodeFile(IFormFile file, [FromQuery] int? templateId)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                var result = _decoder.DecodeBinary(bytes, templateId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding file");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("decode/json")]
        public async Task<ActionResult<List<DecodedMessage>>> DecodeJsonFile(IFormFile file)
        {
            var tempPath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                
                var results = _decoder.DecodeJsonFile(tempPath);
                
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding JSON file");
                return BadRequest(new { error = ex.Message });
            }
            finally
            {
                // Ensure temp file is always deleted
                if (System.IO.File.Exists(tempPath))
                {
                    try
                    {
                        System.IO.File.Delete(tempPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file: {TempPath}", tempPath);
                    }
                }
            }
        }

        [HttpGet("health")]
        public ActionResult Health()
        {
            return Ok(new { status = "healthy", service = "FAST Message Decoder API" });
        }
    }

    public class Base64Request
    {
        public required string Data { get; set; }
        public int? TemplateId { get; set; }
    }

    public class HexRequest
    {
        public required string Data { get; set; }
        public int? TemplateId { get; set; }
    }
}
