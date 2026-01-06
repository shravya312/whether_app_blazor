using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace WeatherApp.Client.Services
{
    public class PushNotificationService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public PushNotificationService(IJSRuntime jsRuntime, HttpClient httpClient, IConfiguration configuration)
        {
            _jsRuntime = jsRuntime;
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5009";
        }

        public async Task<bool> IsSupportedAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<bool>("eval", 
                    "'serviceWorker' in navigator && 'PushManager' in window");
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetPermissionStatusAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("eval",
                    "typeof Notification !== 'undefined' ? Notification.permission : 'denied'");
            }
            catch
            {
                return "denied";
            }
        }

        public async Task<bool> RequestPermissionAsync()
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<string>("PushNotification.requestPermission");
                return result == "granted";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting permission: {ex.Message}");
                return false;
            }
        }

        public async Task<PushSubscriptionDto?> SubscribeAsync(string userId)
        {
            try
            {
                if (!await IsSupportedAsync())
                {
                    Console.WriteLine("Push notifications not supported");
                    throw new InvalidOperationException("Push notifications are not supported in this browser. Please use a modern browser like Chrome, Firefox, or Edge.");
                }

                Console.WriteLine($"Attempting to subscribe user {userId} with API URL: {_apiBaseUrl}");
                
                // Pass API base URL to JavaScript
                // Use InvokeAsync with error handling to get JavaScript errors
                PushSubscriptionDto? subscription = null;
                try
                {
                    subscription = await _jsRuntime.InvokeAsync<PushSubscriptionDto>("PushNotification.subscribe", userId, _apiBaseUrl);
                }
                catch (JSException jsEx)
                {
                    Console.WriteLine($"JavaScript error during subscription: {jsEx.Message}");
                    throw new InvalidOperationException($"Subscription failed: {jsEx.Message}. Please check the browser console for details.");
                }
                
                if (subscription != null)
                {
                    Console.WriteLine($"Subscription received, saving to backend...");
                    // Save subscription to backend
                    await SaveSubscriptionToBackendAsync(userId, subscription);
                    Console.WriteLine($"Subscription saved successfully");
                }
                else
                {
                    throw new InvalidOperationException("Subscription returned null. Check browser console for JavaScript errors.");
                }

                return subscription;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error subscribing to push: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to let caller handle it
            }
        }

        public async Task<bool> UnsubscribeAsync(string userId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("PushNotification.unsubscribe");
                
                // Remove subscription from backend
                await RemoveSubscriptionFromBackendAsync(userId);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unsubscribing from push: {ex.Message}");
                return false;
            }
        }

        public async Task<PushSubscriptionDto?> GetSubscriptionAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<PushSubscriptionDto>("PushNotification.getSubscription");
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveSubscriptionToBackendAsync(string userId, PushSubscriptionDto subscription)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/PushNotification/subscribe",
                    new { UserId = userId, Subscription = subscription });
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to save subscription: {response.StatusCode} - {errorContent}");
                }
                else
                {
                    Console.WriteLine($"Successfully saved subscription for user {userId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving subscription to backend: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task RemoveSubscriptionFromBackendAsync(string userId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/api/PushNotification/unsubscribe",
                    new { UserId = userId });
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to remove subscription: {response.StatusCode} - {errorContent}");
                }
                else
                {
                    Console.WriteLine($"Successfully removed subscription for user {userId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing subscription from backend: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    public class PushSubscriptionDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public Dictionary<string, string> Keys { get; set; } = new();
    }
}

