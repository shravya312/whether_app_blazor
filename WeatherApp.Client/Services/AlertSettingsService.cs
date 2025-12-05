using WeatherApp.Client.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace WeatherApp.Client.Services
{
    public class AlertSettingsService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "alert_settings";

        public AlertSettingsService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<AlertSettings> GetAlertSettingsAsync(string userId)
        {
            try
            {
                var key = $"{StorageKey}_{userId}";
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                
                if (string.IsNullOrEmpty(json))
                {
                    return new AlertSettings
                    {
                        UserId = userId,
                        MaxTemperature = 35,
                        MinTemperature = -10,
                        MaxWindSpeed = 20,
                        EnableThunderstormAlerts = true,
                        EnableHeavyRainAlerts = true,
                        EnableHeavySnowAlerts = true
                    };
                }

                var settings = JsonSerializer.Deserialize<AlertSettings>(json);
                return settings ?? new AlertSettings { UserId = userId };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching alert settings: {ex.Message}");
                return new AlertSettings { UserId = userId };
            }
        }

        public async Task<bool> SaveAlertSettingsAsync(AlertSettings settings)
        {
            try
            {
                var key = $"{StorageKey}_{settings.UserId}";
                var json = JsonSerializer.Serialize(settings);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving alert settings: {ex.Message}");
                return false;
            }
        }
    }
}

