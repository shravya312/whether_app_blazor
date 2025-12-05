namespace WeatherApp.Client.Models
{
    public class WeatherData
    {
        public string? Id { get; set; }
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double Visibility { get; set; }
        public string Description { get; set; } = string.Empty;
        public string MainCondition { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public double Cloudiness { get; set; }
        public DateTime Timestamp { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class ForecastData
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public List<ForecastItem> Items { get; set; } = new();
    }

    public class ForecastItem
    {
        public DateTime DateTime { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public string Description { get; set; } = string.Empty;
        public string MainCondition { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public double Cloudiness { get; set; }
        public double? Precipitation { get; set; }
    }

    public class WeatherRequest
    {
        public string City { get; set; } = string.Empty;
        public string? Country { get; set; }
    }

    public class LocationRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class FavoriteCity
    {
        public string Id { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
        public int SearchCount { get; set; } = 0;
        public DateTime LastSearched { get; set; } = DateTime.UtcNow;
    }
}

