using Microsoft.AspNetCore.Mvc;
using WeatherApp.API.Services;

namespace WeatherApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly AIMLService _aimlService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(AIMLService aimlService, ILogger<ChatbotController> logger)
        {
            _aimlService = aimlService;
            _logger = logger;
        }

        [HttpPost("query")]
        public async Task<IActionResult> ProcessQuery([FromBody] ChatbotQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { message = "Message is required" });
                }

                _logger.LogInformation($"Chatbot query received: {request.Message}");

                var response = await _aimlService.ProcessQueryAsync(request.Message, request.UserId);

                return Ok(new ChatbotResponse
                {
                    Response = response,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chatbot query");
                return StatusCode(500, new { message = "An error occurred while processing your query." });
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Chatbot API is working!" });
        }
    }

    public class ChatbotQueryRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }

    public class ChatbotResponse
    {
        public string Response { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}

