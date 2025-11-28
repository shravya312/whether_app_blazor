using MongoDB.Driver;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<WeatherData> _weatherCollection;

        public MongoDbService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration["MongoDB:ConnectionString"]);
            var database = client.GetDatabase(configuration["MongoDB:DatabaseName"]);
            _weatherCollection = database.GetCollection<WeatherData>("WeatherData");
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
    }
}

