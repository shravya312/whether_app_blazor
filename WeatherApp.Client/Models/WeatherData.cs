namespace WeatherApp.Client.Models
{
    public class WeatherData
    {
        public string? Id { get; set; }
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double WindSpeed { get; set; }
    }

    public class WeatherRequest
    {
        public string City { get; set; } = string.Empty;
        public string? Country { get; set; }
    }
}

