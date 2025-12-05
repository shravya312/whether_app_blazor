using WeatherApp.Client.Models;
using WeatherApp.Client.Services;
using Microsoft.JSInterop;

namespace WeatherApp.Client.Services
{
    public class FavoriteCitiesMonitorService
    {
        private readonly WeatherApiService _weatherApiService;
        private readonly WeatherAlertsService _alertsService;
        private readonly AlertSettingsService _alertSettingsService;
        private readonly AlertHistoryService _alertHistoryService;
        private readonly FavoriteCitiesService _favoriteCitiesService;
        private readonly SearchedCitiesService _searchedCitiesService;
        private readonly IJSRuntime _jsRuntime;

        public FavoriteCitiesMonitorService(
            WeatherApiService weatherApiService,
            WeatherAlertsService alertsService,
            AlertSettingsService alertSettingsService,
            AlertHistoryService alertHistoryService,
            FavoriteCitiesService favoriteCitiesService,
            SearchedCitiesService searchedCitiesService,
            IJSRuntime jsRuntime)
        {
            _weatherApiService = weatherApiService;
            _alertsService = alertsService;
            _alertSettingsService = alertSettingsService;
            _alertHistoryService = alertHistoryService;
            _favoriteCitiesService = favoriteCitiesService;
            _searchedCitiesService = searchedCitiesService;
            _jsRuntime = jsRuntime;
        }

        public async Task<List<WeatherAlert>> CheckFavoriteCitiesAlertsAsync(string userId)
        {
            var allAlerts = new List<WeatherAlert>();
            
            try
            {
                var settings = await _alertSettingsService.GetAlertSettingsAsync(userId);
                var favoriteCities = await _favoriteCitiesService.GetFavoriteCitiesAsync(userId);
                
                // If MonitoredCities is empty or null, check all favorites (default behavior)
                // Otherwise, only check cities in the MonitoredCities list
                var citiesToCheck = (settings.MonitoredCities == null || !settings.MonitoredCities.Any())
                    ? favoriteCities
                    : favoriteCities.Where(f => settings.MonitoredCities.Contains($"{f.City}, {f.Country}"));

                foreach (var favorite in citiesToCheck)
                {
                    try
                    {
                        var weather = await _weatherApiService.GetWeatherAsync(favorite.City, favorite.Country);
                        if (weather != null)
                        {
                            var forecast = await _weatherApiService.GetForecastAsync(favorite.City, favorite.Country);
                            var alerts = await _alertsService.CheckWeatherAlertsAsync(weather, forecast, settings);
                            
                            // Set country for alerts
                            foreach (var alert in alerts)
                            {
                                alert.Country = favorite.Country;
                            }
                            
                            allAlerts.AddRange(alerts);
                            
                            // Save alerts to history
                            foreach (var alert in alerts)
                            {
                                await _alertHistoryService.SaveAlertAsync(userId, alert);
                                
                                // Show browser notification if enabled
                                if (settings.EnablePushNotifications)
                                {
                                    await ShowNotification(alert);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking alerts for {favorite.City}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking favorite cities alerts: {ex.Message}");
            }

            return allAlerts;
        }

        public async Task<List<WeatherAlert>> CheckAllCitiesAlertsAsync(string userId)
        {
            var allAlerts = new List<WeatherAlert>();
            
            try
            {
                var settings = await _alertSettingsService.GetAlertSettingsAsync(userId);
                var searchedCities = await _searchedCitiesService.GetAllSearchedCitiesAsync(userId);
                
                // Check all searched cities for alerts
                foreach (var city in searchedCities)
                {
                    try
                    {
                        var weather = await _weatherApiService.GetWeatherAsync(city.City, city.Country);
                        if (weather != null)
                        {
                            var forecast = await _weatherApiService.GetForecastAsync(city.City, city.Country);
                            var alerts = await _alertsService.CheckWeatherAlertsAsync(weather, forecast, settings);
                            
                            // Set country for alerts
                            foreach (var alert in alerts)
                            {
                                alert.Country = city.Country;
                            }
                            
                            allAlerts.AddRange(alerts);
                            
                            // Save alerts to history
                            foreach (var alert in alerts)
                            {
                                await _alertHistoryService.SaveAlertAsync(userId, alert);
                                
                                // Show browser notification if enabled
                                if (settings.EnablePushNotifications)
                                {
                                    await ShowNotification(alert);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking alerts for {city.City}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking all cities alerts: {ex.Message}");
            }

            return allAlerts;
        }

        private async Task ShowNotification(WeatherAlert alert)
        {
            try
            {
                var permission = await _jsRuntime.InvokeAsync<string>("eval", 
                    "typeof Notification !== 'undefined' ? Notification.permission : 'denied'");
                
                if (permission == "granted")
                {
                    await _jsRuntime.InvokeVoidAsync("eval", $@"
                        new Notification('{alert.Type} Alert - {alert.City}', {{
                            body: '{alert.Message}',
                            icon: '/favicon.ico',
                            tag: '{alert.Id}',
                            requireInteraction: true
                        }});
                    ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing notification: {ex.Message}");
            }
        }
    }
}

