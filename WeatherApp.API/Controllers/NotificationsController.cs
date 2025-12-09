using Microsoft.AspNetCore.Mvc;
using WeatherApp.API.Services;

namespace WeatherApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(EmailService emailService, ILogger<NotificationsController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("email/weather-alert")]
        public async Task<IActionResult> SendWeatherAlertEmail([FromBody] WeatherAlertEmailRequest request)
        {
            _logger.LogInformation($"[NotificationsController] Received email request for {request.ToEmail}");
            Console.WriteLine($"[NotificationsController] 📧 Email request received");
            Console.WriteLine($"[NotificationsController] 📧 To: {request.ToEmail}");
            Console.WriteLine($"[NotificationsController] 📧 City: {request.City}, Country: {request.Country}");
            Console.WriteLine($"[NotificationsController] 📧 Alert Type: {request.AlertType}");
            
            if (string.IsNullOrEmpty(request.ToEmail) || string.IsNullOrEmpty(request.City))
            {
                _logger.LogWarning("[NotificationsController] Invalid request: Missing email or city");
                Console.WriteLine("[NotificationsController] ❌ Bad Request: Email or city is missing");
                return BadRequest(new { message = "Email and city are required" });
            }

            try
            {
                var success = await _emailService.SendWeatherAlertEmailAsync(
                    request.ToEmail,
                    request.City,
                    request.Country ?? "",
                    request.AlertMessage ?? "",
                    request.AlertType ?? "Weather Alert"
                );

                if (success)
                {
                    _logger.LogInformation($"[NotificationsController] ✅ Email sent successfully to {request.ToEmail}");
                    Console.WriteLine($"[NotificationsController] ✅ Email sent successfully");
                    return Ok(new { message = $"Email sent successfully to {request.ToEmail}" });
                }
                else
                {
                    _logger.LogError($"[NotificationsController] ❌ Failed to send email to {request.ToEmail}");
                    Console.WriteLine($"[NotificationsController] ❌ Failed to send email - check API logs for details");
                    return StatusCode(500, new { message = "Failed to send email. Please check API console logs for details." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[NotificationsController] Exception sending email to {request.ToEmail}");
                Console.WriteLine($"[NotificationsController] ❌ Exception: {ex.Message}");
                return StatusCode(500, new { message = $"Error sending email: {ex.Message}" });
            }
        }
    }

    public class WeatherAlertEmailRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string AlertMessage { get; set; } = string.Empty;
        public string? AlertType { get; set; }
    }
}

