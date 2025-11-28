using System.Text.Json;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly MongoDbService _mongoDbService;
        private readonly IConfiguration _configuration;

        public WeatherService(HttpClient httpClient, MongoDbService mongoDbService, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _mongoDbService = mongoDbService;
            _configuration = configuration;
        }

        public async Task<WeatherData?> GetWeatherAsync(string city, string? country = null)
        {
            try
            {
                // Using OpenWeatherMap API (you'll need to get a free API key)
                var apiKey = _configuration["WeatherApi:Key"];
                var trimmedCity = city.Trim();
                var location = string.IsNullOrWhiteSpace(country)
                    ? trimmedCity
                    : $"{trimmedCity},{country.Trim()}";
                var encodedLocation = Uri.EscapeDataString(location);
                var url = $"https://api.openweathermap.org/data/2.5/weather?q={encodedLocation}&appid={apiKey}&units=metric";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    
                    var weatherData = new WeatherData
                    {
                        City = city,
                        Country = country,
                        Temperature = document.RootElement.GetProperty("main").GetProperty("temp").GetDouble(),
                        Humidity = document.RootElement.GetProperty("main").GetProperty("humidity").GetDouble(),
                        Description = document.RootElement.GetProperty("weather")[0].GetProperty("description").GetString() ?? "",
                        WindSpeed = document.RootElement.GetProperty("wind").GetProperty("speed").GetDouble(),
                        Timestamp = DateTime.UtcNow
                    };

                    // Save to MongoDB
                    await _mongoDbService.SaveWeatherDataAsync(weatherData);
                    
                    return weatherData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching weather data: {ex.Message}");
            }
            
            return null;
        }
    }
}

