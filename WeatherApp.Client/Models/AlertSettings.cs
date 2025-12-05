namespace WeatherApp.Client.Models
{
    public class AlertSettings
    {
        public string UserId { get; set; } = string.Empty;
        public double? MaxTemperature { get; set; }
        public double? MinTemperature { get; set; }
        public double? MaxWindSpeed { get; set; }
        public double? MinHumidity { get; set; }
        public double? MaxHumidity { get; set; }
        public bool EnableThunderstormAlerts { get; set; } = true;
        public bool EnableHeavyRainAlerts { get; set; } = true;
        public bool EnableHeavySnowAlerts { get; set; } = true;
        public bool EnablePushNotifications { get; set; } = false;
        public bool EnableEmailNotifications { get; set; } = false;
        public List<string> MonitoredCities { get; set; } = new();
    }
}

