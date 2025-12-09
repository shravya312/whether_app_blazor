using System.Net.Http.Json;

namespace WeatherApp.Client.Services
{
    public class EmailNotificationService
    {
        private readonly HttpClient _httpClient;

        public EmailNotificationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> SendWeatherAlertEmailAsync(string toEmail, string city, string country, string alertMessage, string alertType)
        {
            try
            {
                Console.WriteLine($"[EmailNotificationService] 📧 Preparing to send email to {toEmail}");
                Console.WriteLine($"[EmailNotificationService] 📧 City: {city}, Country: {country}");
                Console.WriteLine($"[EmailNotificationService] 📧 Alert Type: {alertType}");
                
                var request = new
                {
                    ToEmail = toEmail,
                    City = city,
                    Country = country,
                    AlertMessage = alertMessage,
                    AlertType = alertType
                };

                Console.WriteLine($"[EmailNotificationService] 📧 Calling API: api/notifications/email/weather-alert");
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
                    return false;
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
                return false;
            }
        }
    }
}

