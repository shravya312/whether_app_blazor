using WeatherApp.Client.Models;
using WeatherApp.Client.Services;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

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
        private readonly EmailNotificationService? _emailNotificationService;
        private readonly SupabaseService? _supabaseService;
        private readonly HttpClient? _httpClient;
        private readonly IConfiguration? _configuration;
        private readonly IJSRuntime _jsRuntime;
        private readonly HashSet<string> _emailsSentForAlerts = new(); // Track alerts that have already triggered emails

        public FavoriteCitiesMonitorService(
            WeatherApiService weatherApiService,
            WeatherAlertsService alertsService,
            AlertSettingsService alertSettingsService,
            AlertHistoryService alertHistoryService,
            FavoriteCitiesService favoriteCitiesService,
            SearchedCitiesService searchedCitiesService,
            IJSRuntime jsRuntime,
            EmailNotificationService? emailNotificationService = null,
            SupabaseService? supabaseService = null,
            HttpClient? httpClient = null,
            IConfiguration? configuration = null)
        {
            _weatherApiService = weatherApiService;
            _alertsService = alertsService;
            _alertSettingsService = alertSettingsService;
            _alertHistoryService = alertHistoryService;
            _favoriteCitiesService = favoriteCitiesService;
            _searchedCitiesService = searchedCitiesService;
            _jsRuntime = jsRuntime;
            _emailNotificationService = emailNotificationService;
            _supabaseService = supabaseService;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<WeatherAlert>> CheckFavoriteCitiesAlertsAsync(string userId)
        {
            var allAlerts = new List<WeatherAlert>();
            
            try
            {
                var settings = await _alertSettingsService.GetAlertSettingsAsync(userId);
                var favoriteCities = await _favoriteCitiesService.GetFavoriteCitiesAsync(userId);
                
                // Filter by monitored cities if specified
                var citiesToCheck = (settings.MonitoredCities == null || !settings.MonitoredCities.Any())
                    ? favoriteCities
                    : favoriteCities.Where(f => settings.MonitoredCities.Contains($"{f.City}, {f.Country}"));

                if (!citiesToCheck.Any())
                {
                    Console.WriteLine("[FavoriteCitiesMonitor] ⚠️ No monitored cities to check for alerts");
                    return allAlerts;
                }

                // Get the latest city (most recently searched) - notifications will only be sent for this city
                var latestCity = citiesToCheck.OrderByDescending(f => f.LastSearched).FirstOrDefault();
                
                Console.WriteLine($"[FavoriteCitiesMonitor] 🔍 Checking alerts for ALL {citiesToCheck.Count()} monitored cities");
                Console.WriteLine($"[FavoriteCitiesMonitor] 📢 Notifications will be sent ONLY for latest city: {latestCity?.City}, {latestCity?.Country}");
                
                // Process LATEST city FIRST (for instant alert history update and notifications)
                if (latestCity != null)
                {
                    try
                    {
                        var weather = await _weatherApiService.GetWeatherAsync(latestCity.City, latestCity.Country);
                        if (weather != null)
                        {
                            var forecast = await _weatherApiService.GetForecastAsync(latestCity.City, latestCity.Country);
                            var alerts = await _alertsService.CheckWeatherAlertsAsync(weather, forecast, settings);
                            
                            // Set country for alerts
                            foreach (var alert in alerts)
                            {
                                alert.Country = latestCity.Country;
                            }
                            
                            allAlerts.AddRange(alerts);
                            
                            // Get the latest alert (most recent timestamp) for email notification
                            var latestAlert = alerts.OrderByDescending(a => a.Timestamp).FirstOrDefault();
                            
                            // Save alerts to history INSTANTLY for latest city (batch save for better performance)
                            if (alerts.Any())
                            {
                                await _alertHistoryService.SaveAlertsAsync(userId, alerts);
                            }
                            
                            // Show browser notifications for all alerts from latest city
                            foreach (var alert in alerts)
                            {
                                if (settings.EnablePushNotifications)
                                {
                                    await ShowNotification(alert);
                                }
                            }
                            
                            // Send push notification via backend API (works even when app is closed)
                            if (settings.EnablePushNotifications && latestAlert != null && _httpClient != null && _configuration != null)
                            {
                                try
                                {
                                    var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5009";
                                    await _httpClient.PostAsJsonAsync(
                                        $"{apiBaseUrl}/api/PushNotification/send",
                                        new
                                        {
                                            UserId = userId,
                                            Title = $"{latestAlert.Type} Alert - {latestAlert.City}",
                                            Body = latestAlert.Message,
                                            Icon = "/favicon.ico",
                                            Data = new Dictionary<string, object>
                                            {
                                                { "type", "weather-alert" },
                                                { "alertType", latestAlert.Type.ToString() },
                                                { "city", latestAlert.City },
                                                { "message", latestAlert.Message },
                                                { "url", "/" }
                                            }
                                        });
                                    Console.WriteLine($"[FavoriteCitiesMonitor] 📱 Push notification sent for alert: {latestAlert.Type} in {latestAlert.City}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[FavoriteCitiesMonitor] ⚠️ Error sending push notification: {ex.Message}");
                                }
                            }
                            
                            // Send email notification ONLY for the latest alert from latest city (ONCE per alert cycle)
                            if (settings.EnableEmailNotifications && 
                                _emailNotificationService != null && 
                                _supabaseService != null &&
                                latestAlert != null &&
                                !_emailsSentForAlerts.Contains(latestAlert.Id)) // Only send if not already sent
                            {
                                var user = _supabaseService.GetCurrentUser();
                                if (user?.Email != null)
                                {
                                    Console.WriteLine($"[FavoriteCitiesMonitor] 📧 Sending email for LATEST alert: {latestAlert.Type} in {latestAlert.City} (Timestamp: {latestAlert.Timestamp}, ID: {latestAlert.Id})");
                                    var emailSent = await _emailNotificationService.SendWeatherAlertEmailAsync(
                                        user.Email,
                                        latestAlert.City,
                                        latestAlert.Country,
                                        latestAlert.Message,
                                        latestAlert.Type.ToString()
                                    );
                                    if (emailSent)
                                    {
                                        // Mark this alert as having sent email
                                        _emailsSentForAlerts.Add(latestAlert.Id);
                                        Console.WriteLine($"[FavoriteCitiesMonitor] ✅ Email sent successfully for latest alert (ID: {latestAlert.Id})");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[FavoriteCitiesMonitor] ❌ Failed to send email for latest alert");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking alerts for {latestCity.City}: {ex.Message}");
                    }
                }
                
                // Process OTHER cities in PARALLEL (check only, NO history save, NO notifications)
                // Alert history will ONLY be updated with latest city's alerts
                var otherCities = citiesToCheck.Where(f => 
                    latestCity == null || 
                    f.City != latestCity.City || 
                    f.Country != latestCity.Country).ToList();
                
                if (otherCities.Any())
                {
                    Console.WriteLine($"[FavoriteCitiesMonitor] ⚡ Checking {otherCities.Count} other cities (alerts NOT saved to history - only latest city alerts are saved)");
                    
                    var otherCitiesTasks = otherCities.Select(async favorite =>
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
                                
                                return alerts;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error checking alerts for {favorite.City}: {ex.Message}");
                        }
                        return new List<WeatherAlert>();
                    });
                    
                    // Wait for all cities to process in parallel
                    var otherCitiesResults = await Task.WhenAll(otherCitiesTasks);
                    
                    // Collect alerts from other cities (for return value only, NOT saved to history)
                    foreach (var alerts in otherCitiesResults)
                    {
                        allAlerts.AddRange(alerts);
                    }
                    
                    Console.WriteLine($"[FavoriteCitiesMonitor] ✅ Completed checking {otherCities.Count} other cities (alerts NOT saved to history)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking favorite cities alerts: {ex.Message}");
            }

            return allAlerts;
        }

        public async Task<List<WeatherAlert>> CheckAllCitiesAlertsAsync(string userId, string? currentCity = null, string? currentCountry = null)
        {
            var allAlerts = new List<WeatherAlert>();
            
            try
            {
                var settings = await _alertSettingsService.GetAlertSettingsAsync(userId);
                var searchedCities = await _searchedCitiesService.GetAllSearchedCitiesAsync(userId);
                
                if (!searchedCities.Any())
                {
                    Console.WriteLine("[FavoriteCitiesMonitor] ⚠️ No searched cities to check for alerts");
                    return allAlerts;
                }

                // Prioritize CURRENTLY searched city if provided, otherwise use most recently searched
                SearchedCity? latestCity = null;
                if (!string.IsNullOrEmpty(currentCity) && !string.IsNullOrEmpty(currentCountry))
                {
                    // Find the currently searched city in the list
                    latestCity = searchedCities.FirstOrDefault(c => 
                        c.City.Equals(currentCity, StringComparison.OrdinalIgnoreCase) && 
                        c.Country.Equals(currentCountry, StringComparison.OrdinalIgnoreCase));
                    
                    if (latestCity != null)
                    {
                        Console.WriteLine($"[FavoriteCitiesMonitor] 🎯 Using CURRENTLY searched city: {latestCity.City}, {latestCity.Country}");
                    }
                    else
                    {
                        // City not found in list - create a temporary entry for alert checking
                        Console.WriteLine($"[FavoriteCitiesMonitor] ⚠️ City {currentCity}, {currentCountry} not found in searched cities list. Creating temporary entry.");
                        latestCity = new SearchedCity
                        {
                            City = currentCity,
                            Country = currentCountry,
                            LastSearched = DateTime.UtcNow,
                            SearchCount = 1
                        };
                        Console.WriteLine($"[FavoriteCitiesMonitor] 🎯 Using CURRENTLY searched city (temporary): {latestCity.City}, {latestCity.Country}");
                    }
                }
                
                // Fallback to most recently searched if current city not provided
                if (latestCity == null)
                {
                    // Get the absolute most recent city by LastSearched timestamp
                    latestCity = searchedCities.OrderByDescending(c => c.LastSearched).FirstOrDefault();
                    
                    // Debug: Show top 3 most recent cities
                    var topCities = searchedCities.OrderByDescending(c => c.LastSearched).Take(3).ToList();
                    Console.WriteLine($"[FavoriteCitiesMonitor] 🔍 Top 3 most recent cities:");
                    foreach (var city in topCities)
                    {
                        Console.WriteLine($"[FavoriteCitiesMonitor]   - {city.City}, {city.Country}: {city.LastSearched:yyyy-MM-dd HH:mm:ss.fff}");
                    }
                }
                
                Console.WriteLine($"[FavoriteCitiesMonitor] 🔍 Checking alerts for ALL {searchedCities.Count} searched cities");
                Console.WriteLine($"[FavoriteCitiesMonitor] 📢 Notifications will be sent ONLY for latest city: {latestCity?.City}, {latestCity?.Country} (LastSearched: {latestCity?.LastSearched:yyyy-MM-dd HH:mm:ss.fff})");
                
                // Process LATEST city FIRST (for instant alert history update and notifications)
                if (latestCity != null)
                {
                    try
                    {
                        var weather = await _weatherApiService.GetWeatherAsync(latestCity.City, latestCity.Country);
                        if (weather != null)
                        {
                            var forecast = await _weatherApiService.GetForecastAsync(latestCity.City, latestCity.Country);
                            var alerts = await _alertsService.CheckWeatherAlertsAsync(weather, forecast, settings);
                            
                            // Set country for alerts
                            foreach (var alert in alerts)
                            {
                                alert.Country = latestCity.Country;
                            }
                            
                            allAlerts.AddRange(alerts);
                            
                            // Get the latest alert (most recent timestamp) for email notification
                            var latestAlert = alerts.OrderByDescending(a => a.Timestamp).FirstOrDefault();
                            
                            // Save alerts to history INSTANTLY for latest city (batch save for better performance)
                            if (alerts.Any())
                            {
                                await _alertHistoryService.SaveAlertsAsync(userId, alerts);
                            }
                            
                            // Show browser notifications for all alerts from latest city
                            foreach (var alert in alerts)
                            {
                                if (settings.EnablePushNotifications)
                                {
                                    await ShowNotification(alert);
                                }
                            }
                            
                            // Send push notification via backend API (works even when app is closed)
                            if (settings.EnablePushNotifications && latestAlert != null && _httpClient != null && _configuration != null)
                            {
                                try
                                {
                                    var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5009";
                                    await _httpClient.PostAsJsonAsync(
                                        $"{apiBaseUrl}/api/PushNotification/send",
                                        new
                                        {
                                            UserId = userId,
                                            Title = $"{latestAlert.Type} Alert - {latestAlert.City}",
                                            Body = latestAlert.Message,
                                            Icon = "/favicon.ico",
                                            Data = new Dictionary<string, object>
                                            {
                                                { "type", "weather-alert" },
                                                { "alertType", latestAlert.Type.ToString() },
                                                { "city", latestAlert.City },
                                                { "message", latestAlert.Message },
                                                { "url", "/" }
                                            }
                                        });
                                    Console.WriteLine($"[FavoriteCitiesMonitor] 📱 Push notification sent for alert: {latestAlert.Type} in {latestAlert.City}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[FavoriteCitiesMonitor] ⚠️ Error sending push notification: {ex.Message}");
                                }
                            }
                            
                            // Send email notification ONLY for the latest alert from latest city (ONCE per alert cycle)
                            if (settings.EnableEmailNotifications && 
                                _emailNotificationService != null && 
                                _supabaseService != null &&
                                latestAlert != null &&
                                !_emailsSentForAlerts.Contains(latestAlert.Id)) // Only send if not already sent
                            {
                                var user = _supabaseService.GetCurrentUser();
                                if (user?.Email != null)
                                {
                                    Console.WriteLine($"[FavoriteCitiesMonitor] 📧 Sending email for LATEST alert: {latestAlert.Type} in {latestAlert.City} (Timestamp: {latestAlert.Timestamp}, ID: {latestAlert.Id})");
                                    var emailSent = await _emailNotificationService.SendWeatherAlertEmailAsync(
                                        user.Email,
                                        latestAlert.City,
                                        latestAlert.Country,
                                        latestAlert.Message,
                                        latestAlert.Type.ToString()
                                    );
                                    if (emailSent)
                                    {
                                        // Mark this alert as having sent email
                                        _emailsSentForAlerts.Add(latestAlert.Id);
                                        Console.WriteLine($"[FavoriteCitiesMonitor] ✅ Email sent successfully for latest alert (ID: {latestAlert.Id})");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[FavoriteCitiesMonitor] ❌ Failed to send email for latest alert");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking alerts for {latestCity.City}: {ex.Message}");
                    }
                }
                
                // Process OTHER cities in PARALLEL (check only, NO history save, NO notifications)
                // Alert history will ONLY be updated with latest city's alerts
                var otherCities = searchedCities.Where(c => 
                    latestCity == null || 
                    c.City != latestCity.City || 
                    c.Country != latestCity.Country).ToList();
                
                if (otherCities.Any())
                {
                    Console.WriteLine($"[FavoriteCitiesMonitor] ⚡ Checking {otherCities.Count} other cities (alerts NOT saved to history - only latest city alerts are saved)");
                    
                    var otherCitiesTasks = otherCities.Select(async city =>
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
                                
                                return alerts;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error checking alerts for {city.City}: {ex.Message}");
                        }
                        return new List<WeatherAlert>();
                    });
                    
                    // Wait for all cities to process in parallel
                    var otherCitiesResults = await Task.WhenAll(otherCitiesTasks);
                    
                    // Collect alerts from other cities (for return value only, NOT saved to history)
                    foreach (var alerts in otherCitiesResults)
                    {
                        allAlerts.AddRange(alerts);
                    }
                    
                    Console.WriteLine($"[FavoriteCitiesMonitor] ✅ Completed checking {otherCities.Count} other cities (alerts NOT saved to history)");
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

