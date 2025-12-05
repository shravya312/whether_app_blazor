using WeatherApp.Client.Models;
using WeatherApp.Client.Services;
using Microsoft.JSInterop;
using System.Text.Json;

namespace WeatherApp.Client.Services
{
    public class AlertHistoryService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "alert_history";

        public AlertHistoryService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<List<WeatherAlert>> GetAlertHistoryAsync(string userId, int? limit = null)
        {
            try
            {
                var key = $"{StorageKey}_{userId}";
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                
                if (string.IsNullOrEmpty(json))
                    return new List<WeatherAlert>();

                var alerts = JsonSerializer.Deserialize<List<WeatherAlert>>(json);
                if (alerts == null)
                    return new List<WeatherAlert>();

                var sorted = alerts.OrderByDescending(a => a.Timestamp).ToList();
                
                if (limit.HasValue)
                {
                    return sorted.Take(limit.Value).ToList();
                }
                
                return sorted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching alert history: {ex.Message}");
                return new List<WeatherAlert>();
            }
        }

        public async Task SaveAlertAsync(string userId, WeatherAlert alert)
        {
            try
            {
                var history = await GetAlertHistoryAsync(userId);
                
                // Add alert with unique ID if not present
                if (!history.Any(a => a.Id == alert.Id && a.Timestamp == alert.Timestamp))
                {
                    history.Add(alert);
                    
                    // Keep only last 1000 alerts
                    if (history.Count > 1000)
                    {
                        history = history.OrderByDescending(a => a.Timestamp).Take(1000).ToList();
                    }
                    
                    var key = $"{StorageKey}_{userId}";
                    var json = JsonSerializer.Serialize(history);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving alert to history: {ex.Message}");
            }
        }

        public async Task ClearAlertHistoryAsync(string userId)
        {
            try
            {
                var key = $"{StorageKey}_{userId}";
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing alert history: {ex.Message}");
            }
        }
    }
}

