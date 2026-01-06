using Microsoft.AspNetCore.Mvc;
using WeatherApp.API.Services;

namespace WeatherApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PushNotificationController : ControllerBase
    {
        private readonly PushNotificationService _pushService;
        private readonly ILogger<PushNotificationController> _logger;

        public PushNotificationController(
            PushNotificationService pushService,
            ILogger<PushNotificationController> logger)
        {
            _pushService = pushService;
            _logger = logger;
        }

        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            try
            {
                var publicKey = _pushService.GetVapidPublicKey();
                // Ensure the key is trimmed and has no extra whitespace
                var cleanKey = publicKey.Trim();
                // Return as plain text with proper content type
                return Content(cleanKey, "text/plain; charset=utf-8");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting VAPID public key");
                return StatusCode(500, "VAPID public key not configured");
            }
        }

        [HttpPost("subscribe")]
        public IActionResult Subscribe([FromBody] SubscribeRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) || request.Subscription == null)
                {
                    return BadRequest("UserId and Subscription are required");
                }

                _pushService.AddSubscription(request.UserId, request.Subscription);
                return Ok(new { success = true, message = "Subscription added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to push notifications");
                return StatusCode(500, "Failed to subscribe");
            }
        }

        [HttpPost("unsubscribe")]
        public IActionResult Unsubscribe([FromBody] UnsubscribeRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest("UserId is required");
                }

                _pushService.RemoveSubscription(request.UserId);
                return Ok(new { success = true, message = "Unsubscribed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from push notifications");
                return StatusCode(500, "Failed to unsubscribe");
            }
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Body))
                {
                    return BadRequest("UserId, Title, and Body are required");
                }

                await _pushService.SendNotificationAsync(
                    request.UserId,
                    request.Title,
                    request.Body,
                    request.Icon,
                    request.Data);

                return Ok(new { success = true, message = "Notification sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification");
                return StatusCode(500, "Failed to send notification");
            }
        }
    }

    public class SubscribeRequest
    {
        public string UserId { get; set; } = string.Empty;
        public PushSubscriptionDto Subscription { get; set; } = new();
    }

    public class UnsubscribeRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class SendNotificationRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }
}

