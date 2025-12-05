using WeatherApp.Client.Models;

namespace WeatherApp.Client.Services
{
    public class WeatherAlertsService
    {
        public List<WeatherAlert> CheckWeatherAlerts(WeatherData weather, ForecastData? forecast = null)
        {
            var alerts = new List<WeatherAlert>();

            if (weather == null) return alerts;

            // Check for severe temperature
            if (weather.Temperature > 35)
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.SevereHeat,
                    Severity = AlertSeverity.High,
                    Message = $"Extreme heat warning: Temperature is {weather.Temperature:F1}°C",
                    City = weather.City,
                    Timestamp = DateTime.UtcNow
                });
            }
            else if (weather.Temperature < -10)
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.SevereCold,
                    Severity = AlertSeverity.High,
                    Message = $"Extreme cold warning: Temperature is {weather.Temperature:F1}°C",
                    City = weather.City,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check for severe wind
            if (weather.WindSpeed > 20)
            {
                alerts.Add(new WeatherAlert
                {
                    Type = AlertType.HighWind,
                    Severity = AlertSeverity.Medium,
                    Message = $"High wind warning: Wind speed is {weather.WindSpeed:F1} m/s",
                    City = weather.City,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check for severe weather conditions
            var mainCondition = weather.MainCondition.ToLower();
            if (mainCondition.Contains("thunderstorm"))
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
            else if (mainCondition.Contains("snow") && weather.Temperature < 0)
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
            else if (mainCondition.Contains("rain") && weather.Humidity > 80)
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
                    if (item.MainCondition.ToLower().Contains("thunderstorm"))
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
    }

    public class WeatherAlert
    {
        public AlertType Type { get; set; }
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public enum AlertType
    {
        SevereHeat,
        SevereCold,
        HighWind,
        Thunderstorm,
        HeavyRain,
        HeavySnow,
        Fog
    }

    public enum AlertSeverity
    {
        Low,
        Medium,
        High
    }
}

