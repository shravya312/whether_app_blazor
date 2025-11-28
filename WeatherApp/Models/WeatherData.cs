using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WeatherApp.Models
{
    public class WeatherData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double WindSpeed { get; set; }
    }

    public class WeatherRequest
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = "US";
    }
}

