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
        private readonly EmailNotificationService? _emailNotificationService;
        private readonly SupabaseService? _supabaseService;
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
            SupabaseService? supabaseService = null)
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
                
                // Process OTHER cities in PARALLEL (save to history only, NO notifications) - Much faster!
                var otherCities = citiesToCheck.Where(f => 
                    latestCity == null || 
                    f.City != latestCity.City || 
                    f.Country != latestCity.Country).ToList();
                
                if (otherCities.Any())
                {
                    Console.WriteLine($"[FavoriteCitiesMonitor] ⚡ Processing {otherCities.Count} other cities in parallel for history...");
                    
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
                    
                    // Collect all alerts from other cities
                    var allOtherCityAlerts = new List<WeatherAlert>();
                    foreach (var alerts in otherCitiesResults)
                    {
                        allOtherCityAlerts.AddRange(alerts);
                        allAlerts.AddRange(alerts);
                    }
                    
                    // Batch save all alerts from other cities at once (much faster and prevents race conditions)
                    if (allOtherCityAlerts.Any())
                    {
                        await _alertHistoryService.SaveAlertsAsync(userId, allOtherCityAlerts);
                    }
                    
                    Console.WriteLine($"[FavoriteCitiesMonitor] ✅ Completed processing {otherCities.Count} other cities");
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
                
                if (!searchedCities.Any())
                {
                    Console.WriteLine("[FavoriteCitiesMonitor] ⚠️ No searched cities to check for alerts");
                    return allAlerts;
                }

                // Get the latest city (most recently searched) - notifications will only be sent for this city
                var latestCity = searchedCities.OrderByDescending(c => c.LastSearched).FirstOrDefault();
                
                Console.WriteLine($"[FavoriteCitiesMonitor] 🔍 Checking alerts for ALL {searchedCities.Count} searched cities");
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
                
                // Process OTHER cities in PARALLEL (save to history only, NO notifications) - Much faster!
                var otherCities = searchedCities.Where(c => 
                    latestCity == null || 
                    c.City != latestCity.City || 
                    c.Country != latestCity.Country).ToList();
                
                if (otherCities.Any())
                {
                    Console.WriteLine($"[FavoriteCitiesMonitor] ⚡ Processing {otherCities.Count} other cities in parallel for history...");
                    
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
                    
                    // Collect all alerts from other cities
                    var allOtherCityAlerts = new List<WeatherAlert>();
                    foreach (var alerts in otherCitiesResults)
                    {
                        allOtherCityAlerts.AddRange(alerts);
                        allAlerts.AddRange(alerts);
                    }
                    
                    // Batch save all alerts from other cities at once (much faster and prevents race conditions)
                    if (allOtherCityAlerts.Any())
                    {
                        await _alertHistoryService.SaveAlertsAsync(userId, allOtherCityAlerts);
                    }
                    
                    Console.WriteLine($"[FavoriteCitiesMonitor] ✅ Completed processing {otherCities.Count} other cities");
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

