using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WeatherApp.API.Models
{
    public class CitySearchStat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("city")]
        public string City { get; set; } = string.Empty;

        [BsonElement("count")]
        public int Count { get; set; }

        [BsonElement("lastSearched")]
        public DateTime LastSearched { get; set; } = DateTime.UtcNow;
    }
}

