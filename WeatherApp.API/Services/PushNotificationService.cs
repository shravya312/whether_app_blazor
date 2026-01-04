using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebPush;

namespace WeatherApp.API.Services
{
    public class PushNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PushNotificationService> _logger;
        private readonly Dictionary<string, PushSubscription> _subscriptions = new();

        public PushNotificationService(IConfiguration configuration, ILogger<PushNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GetVapidPublicKey()
        {
            var publicKey = _configuration["PushNotifications:VapidPublicKey"];
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new InvalidOperationException("VAPID public key is not configured. Please set PushNotifications:VapidPublicKey in appsettings.json");
            }
            return publicKey;
        }

        public void AddSubscription(string userId, PushSubscriptionDto subscriptionDto)
        {
            var subscription = new PushSubscription(
                subscriptionDto.Endpoint,
                subscriptionDto.Keys["p256dh"],
                subscriptionDto.Keys["auth"]
            );

            var key = $"{userId}_{subscriptionDto.Endpoint}";
            _subscriptions[key] = subscription;
            
            _logger.LogInformation($"Added push subscription for user {userId}");
        }

        public void RemoveSubscription(string userId, string? endpoint = null)
        {
            if (!string.IsNullOrEmpty(endpoint))
            {
                var key = $"{userId}_{endpoint}";
                _subscriptions.Remove(key);
                _logger.LogInformation($"Removed push subscription for user {userId}");
            }
            else
            {
                // Remove all subscriptions for this user
                var keysToRemove = _subscriptions.Keys
                    .Where(k => k.StartsWith($"{userId}_"))
                    .ToList();
                
                foreach (var key in keysToRemove)
                {
                    _subscriptions.Remove(key);
                }
                
                _logger.LogInformation($"Removed all push subscriptions for user {userId}");
            }
        }

        public List<PushSubscription> GetUserSubscriptions(string userId)
        {
            return _subscriptions
                .Where(kvp => kvp.Key.StartsWith($"{userId}_"))
                .Select(kvp => kvp.Value)
                .ToList();
        }

        public async Task SendNotificationAsync(string userId, string title, string body, string? icon = null, Dictionary<string, object>? data = null)
        {
            var subscriptions = GetUserSubscriptions(userId);
            
            if (!subscriptions.Any())
            {
                _logger.LogWarning($"No push subscriptions found for user {userId}");
                return;
            }

            var publicKey = GetVapidPublicKey();
            var privateKey = _configuration["PushNotifications:VapidPrivateKey"];
            
            if (string.IsNullOrEmpty(privateKey))
            {
                throw new InvalidOperationException("VAPID private key is not configured. Please set PushNotifications:VapidPrivateKey in appsettings.json");
            }

            var vapidDetails = new VapidDetails(
                _configuration["PushNotifications:VapidSubject"] ?? "mailto:admin@weatherapp.com",
                publicKey,
                privateKey
            );

            var payload = JsonSerializer.Serialize(new
            {
                title = title,
                body = body,
                icon = icon ?? "/favicon.ico",
                tag = Guid.NewGuid().ToString(),
                data = data ?? new Dictionary<string, object>()
            });

            var webPushClient = new WebPushClient();

            foreach (var subscription in subscriptions)
            {
                try
                {
                    await webPushClient.SendNotificationAsync(subscription, payload, vapidDetails);
                    _logger.LogInformation($"Push notification sent to user {userId}");
                }
                catch (WebPushException ex)
                {
                    _logger.LogError(ex, $"Failed to send push notification to user {userId}: {ex.Message}");
                    
                    // Remove invalid subscriptions
                    if (ex.StatusCode == System.Net.HttpStatusCode.Gone || 
                        ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        RemoveSubscription(userId, subscription.Endpoint);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending push notification to user {userId}: {ex.Message}");
                }
            }
        }

        public async Task SendWeatherAlertAsync(string userId, string alertType, string city, string message)
        {
            var title = $"{alertType} Alert - {city}";
            var data = new Dictionary<string, object>
            {
                { "type", "weather-alert" },
                { "alertType", alertType },
                { "city", city },
                { "message", message },
                { "url", "/" }
            };

            await SendNotificationAsync(userId, title, message, "/favicon.ico", data);
        }
    }

    public class PushSubscriptionDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public Dictionary<string, string> Keys { get; set; } = new();
    }
}

