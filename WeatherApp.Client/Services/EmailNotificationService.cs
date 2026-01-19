using System.Net.Http.Json;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;

namespace WeatherApp.Client.Services
{
    public class EmailNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        private readonly IConfiguration _configuration;

        public EmailNotificationService(HttpClient httpClient, IJSRuntime jsRuntime, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _configuration = configuration;
        }

        public async Task<bool> SendWeatherAlertEmailAsync(string toEmail, string city, string country, string alertMessage, string alertType)
        {
            try
            {
                Console.WriteLine($"[EmailNotificationService] 📧 Preparing to send email to {toEmail}");
                Console.WriteLine($"[EmailNotificationService] 📧 City: {city}, Country: {country}");
                Console.WriteLine($"[EmailNotificationService] 📧 Alert Type: {alertType}");
                
                // Check if online using JavaScript
                var isOnline = await _jsRuntime.InvokeAsync<bool>("eval", "navigator.onLine");
                
                if (!isOnline)
                {
                    // Queue email for later when online
                    // Background Sync will automatically send when connection is restored
                    Console.WriteLine($"[EmailNotificationService] ⚠️ Offline - Queueing email for automatic delivery when online");
                    await QueueEmailOffline(toEmail, city, country, alertMessage, alertType);
                    // Return true - email is queued and will be sent automatically when online
                    return true;
                }
                
                var request = new
                {
                    ToEmail = toEmail,
                    City = city,
                    Country = country,
                    AlertMessage = alertMessage,
                    AlertType = alertType
                };

                Console.WriteLine($"[EmailNotificationService] 📧 Calling API: api/notifications/email/weather-alert");
                Console.WriteLine($"[EmailNotificationService] 📧 API Base URL: {_httpClient.BaseAddress}");
                
                try
                {
                    var response = await _httpClient.PostAsJsonAsync("api/notifications/email/weather-alert", request);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[EmailNotificationService] ✅ Email API call successful: {result}");
                        return true;
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[EmailNotificationService] ❌ Email API call failed: {response.StatusCode}");
                        Console.WriteLine($"[EmailNotificationService] ❌ Error: {errorContent}");
                        
                        // If network error, queue for retry
                        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                            response.StatusCode == System.Net.HttpStatusCode.GatewayTimeout ||
                            response.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
                        {
                            Console.WriteLine($"[EmailNotificationService] ⚠️ Network error - Queueing email for retry");
                            await QueueEmailOffline(toEmail, city, country, alertMessage, alertType);
                            return true;
                        }
                        
                        return false;
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    // Network error - queue for retry
                    Console.WriteLine($"[EmailNotificationService] ⚠️ Network exception - Queueing email for retry: {httpEx.Message}");
                    await QueueEmailOffline(toEmail, city, country, alertMessage, alertType);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailNotificationService] ❌ ERROR sending email notification: {ex.Message}");
                Console.WriteLine($"[EmailNotificationService] ❌ Exception Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[EmailNotificationService] ❌ Inner Exception: {ex.InnerException.Message}");
                }
                
                // Try to queue email even on error (might be network issue)
                try
                {
                    await QueueEmailOffline(toEmail, city, country, alertMessage, alertType);
                    Console.WriteLine($"[EmailNotificationService] ✅ Email queued for retry");
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private async Task QueueEmailOffline(string toEmail, string city, string country, string alertMessage, string alertType)
        {
            try
            {
                var apiBaseUrl = _configuration["ApiBaseUrl"] ?? _httpClient.BaseAddress?.ToString() ?? "http://localhost:5009";
                
                await _jsRuntime.InvokeVoidAsync("EmailQueue.queueEmail", new
                {
                    toEmail,
                    city,
                    country,
                    alertMessage,
                    alertType
                });
                
                // Trigger background sync - this will continuously retry until email is sent
                // Background Sync API automatically retries when connection is restored
                try
                {
                    await _jsRuntime.InvokeVoidAsync("eval", @"
                        if ('serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype) {
                            navigator.serviceWorker.ready.then(registration => {
                                registration.sync.register('email-sync')
                                    .then(() => console.log('✅ Email sync registered - will send automatically when online'))
                                    .catch(err => console.log('Email sync registration failed:', err));
                            });
                        } else {
                            console.log('Background Sync not available - email will sync when online event fires');
                        }
                    ");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EmailNotificationService] ⚠️ Background sync registration failed: {ex.Message}");
                    // Will still sync via online event listener
                }
                
                Console.WriteLine($"[EmailNotificationService] ✅ Email queued successfully - will be sent automatically when connection is restored");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailNotificationService] ❌ Failed to queue email: {ex.Message}");
                throw;
            }
        }
    }
}
