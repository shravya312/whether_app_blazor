using MongoDB.Driver;
using WeatherApp.API.Models;

namespace WeatherApp.API.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<WeatherData> _weatherCollection;
        private readonly IMongoCollection<CitySearchStat> _cityStatsCollection;

        public MongoDbService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(configuration["MongoDB:DatabaseName"]);
            _weatherCollection = database.GetCollection<WeatherData>("WeatherData");
            _cityStatsCollection = database.GetCollection<CitySearchStat>("CitySearchStats");
        }

        public async Task<List<WeatherData>> GetWeatherHistoryAsync()
        {
            return await _weatherCollection.Find(_ => true)
                .SortByDescending(x => x.Timestamp)
                .Limit(50)
                .ToListAsync();
        }

        public async Task SaveWeatherDataAsync(WeatherData weatherData)
        {
            await _weatherCollection.InsertOneAsync(weatherData);
        }

        public async Task<WeatherData?> GetLatestWeatherAsync(string city)
        {
            return await _weatherCollection
                .Find(x => x.City.ToLower() == city.ToLower())
                .SortByDescending(x => x.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task IncrementCitySearchAsync(string city)
        {
            var normalizedCity = city.Trim();
            if (string.IsNullOrEmpty(normalizedCity))
            {
                return;
            }

            var filter = Builders<CitySearchStat>.Filter.Eq(x => x.City, normalizedCity);
            var update = Builders<CitySearchStat>.Update
                .SetOnInsert(x => x.City, normalizedCity)
                .Inc(x => x.Count, 1)
                .Set(x => x.LastSearched, DateTime.UtcNow);

            await _cityStatsCollection.UpdateOneAsync(
                filter,
                update,
                new UpdateOptions { IsUpsert = true });
        }

        public async Task<List<CitySearchStat>> GetTopCitySearchesAsync(int limit = 5)
        {
            return await _cityStatsCollection
                .Find(_ => true)
                .SortByDescending(x => x.Count)
                .ThenByDescending(x => x.LastSearched)
                .Limit(limit)
                .ToListAsync();
        }
    }
}

