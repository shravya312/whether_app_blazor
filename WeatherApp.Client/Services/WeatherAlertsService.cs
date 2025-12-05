using WeatherApp.Client.Models;
using Microsoft.JSInterop;

namespace WeatherApp.Client.Services
{
    public class WeatherAlertsService
    {
        private readonly AlertSettingsService? _alertSettingsService;
        private readonly IJSRuntime? _jsRuntime;

        public WeatherAlertsService(AlertSettingsService? alertSettingsService = null, IJSRuntime? jsRuntime = null)
        {
            _alertSettingsService = alertSettingsService;
            _jsRuntime = jsRuntime;
        }

        public async Task<List<WeatherAlert>> CheckWeatherAlertsAsync(WeatherData weather, ForecastData? forecast = null, AlertSettings? settings = null)
        {
            var alerts = new List<WeatherAlert>();

            if (weather == null) return alerts;

            // Use custom settings if provided, otherwise use defaults
            var maxTemp = settings?.MaxTemperature ?? 35;
            var minTemp = settings?.MinTemperature ?? -10;
            var maxWind = settings?.MaxWindSpeed ?? 20;

            // Check for severe temperature
            if (weather.Temperature > maxTemp)
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.SevereHeat,
                    Severity = AlertSeverity.High,
                    Message = $"Extreme heat warning: Temperature is {weather.Temperature:F1}°C (threshold: {maxTemp}°C)",
                    City = weather.City,
                    Timestamp = DateTime.UtcNow
                });
            }
            else if (weather.Temperature < minTemp)
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.SevereCold,
                    Severity = AlertSeverity.High,
                    Message = $"Extreme cold warning: Temperature is {weather.Temperature:F1}°C (threshold: {minTemp}°C)",
                    City = weather.City,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check for severe wind
            if (weather.WindSpeed > maxWind)
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.HighWind,
                    Severity = AlertSeverity.Medium,
                    Message = $"High wind warning: Wind speed is {weather.WindSpeed:F1} m/s (threshold: {maxWind} m/s)",
                    City = weather.City,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check humidity thresholds
            if (settings != null)
            {
                if (settings.MaxHumidity.HasValue && weather.Humidity > settings.MaxHumidity.Value)
                {
                    alerts.Add(new WeatherAlert
                    {
                        Type = AlertType.HighHumidity,
                        Severity = AlertSeverity.Medium,
                        Message = $"High humidity warning: {weather.Humidity:F0}% (threshold: {settings.MaxHumidity:F0}%)",
                        City = weather.City,
                        Timestamp = DateTime.UtcNow
                    });
                }
                if (settings.MinHumidity.HasValue && weather.Humidity < settings.MinHumidity.Value)
                {
                    alerts.Add(new WeatherAlert
                    {
                        Type = AlertType.LowHumidity,
                        Severity = AlertSeverity.Low,
                        Message = $"Low humidity warning: {weather.Humidity:F0}% (threshold: {settings.MinHumidity:F0}%)",
                        City = weather.City,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            // Check for severe weather conditions
            var mainCondition = weather.MainCondition.ToLower();
            if (mainCondition.Contains("thunderstorm") && (settings == null || settings.EnableThunderstormAlerts))
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.Thunderstorm,
                    Severity = AlertSeverity.High,
                    Message = "Thunderstorm warning: Severe weather conditions expected",
                    City = weather.City,
                    Timestamp = DateTime.UtcNow
                });
            }
            else if (mainCondition.Contains("snow") && weather.Temperature < 0 && (settings == null || settings.EnableHeavySnowAlerts))
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.HeavySnow,
                    Severity = AlertSeverity.Medium,
                    Message = "Heavy snow warning: Snow conditions expected",
                    City = weather.City,
                    Timestamp = DateTime.UtcNow
                });
            }
            else if (mainCondition.Contains("rain") && weather.Humidity > 80 && (settings == null || settings.EnableHeavyRainAlerts))
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.HeavyRain,
                    Severity = AlertSeverity.Medium,
                    Message = "Heavy rain warning: High precipitation expected",
                    City = weather.City,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check forecast for upcoming severe weather
            if (forecast != null)
            {
                foreach (var item in forecast.Items.Take(24))
                {
                    if (item.MainCondition.ToLower().Contains("thunderstorm") && (settings == null || settings.EnableThunderstormAlerts))
                    {
                        alerts.Add(new WeatherAlert
                        {
                            Type = AlertType.Thunderstorm,
                            Severity = AlertSeverity.High,
                            Message = $"Thunderstorm expected at {item.DateTime:g}",
                            City = weather.City,
                            Timestamp = DateTime.UtcNow
                        });
                        break;
                    }
                }
            }

            return alerts;
        }

        // Backward compatibility
        public List<WeatherAlert> CheckWeatherAlerts(WeatherData weather, ForecastData? forecast = null)
        {
            return CheckWeatherAlertsAsync(weather, forecast).GetAwaiter().GetResult();
        }
    }

    public class WeatherAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }

    public enum AlertType
    {
        SevereHeat,
        SevereCold,
        HighWind,
        Thunderstorm,
        HeavyRain,
        HeavySnow,
        Fog,
        HighHumidity,
        LowHumidity
    }

    public enum AlertSeverity
    {
        Low,
        Medium,
        High
    }
}

