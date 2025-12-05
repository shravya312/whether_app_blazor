using System.Globalization;
using System.Text.Json;
using WeatherApp.API.Models;

namespace WeatherApp.API.Services
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
                var apiKey = _configuration["WeatherApi:Key"];
                var trimmedCity = city.Trim();
                var normalizedCity = NormalizeCityName(trimmedCity);
                var location = string.IsNullOrWhiteSpace(country)
                    ? normalizedCity
                    : $"{normalizedCity},{country.Trim()}";
                var encodedLocation = Uri.EscapeDataString(location);
                var url = $"https://api.openweathermap.org/data/2.5/weather?q={encodedLocation}&appid={apiKey}&units=metric";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;
                    var main = root.GetProperty("main");
                    var weather = root.GetProperty("weather")[0];
                    var wind = root.GetProperty("wind");
                    var coord = root.TryGetProperty("coord", out var coordElement) ? coordElement : default;
                    var clouds = root.TryGetProperty("clouds", out var cloudsElement) ? cloudsElement : default;
                    
                    string countryCode = country ?? "";
                    if (string.IsNullOrEmpty(countryCode) && root.TryGetProperty("sys", out var sysElement))
                    {
                        if (sysElement.TryGetProperty("country", out var countryElement))
                        {
                            countryCode = countryElement.GetString() ?? "";
                        }
                    }
                    
                    var weatherData = new WeatherData
                    {
                        City = normalizedCity,
                        Country = countryCode,
                        Temperature = main.GetProperty("temp").GetDouble(),
                        FeelsLike = main.TryGetProperty("feels_like", out var feelsLikeElement) ? feelsLikeElement.GetDouble() : main.GetProperty("temp").GetDouble(),
                        Humidity = main.GetProperty("humidity").GetDouble(),
                        Pressure = main.TryGetProperty("pressure", out var pressureElement) ? pressureElement.GetDouble() : 0,
                        Visibility = root.TryGetProperty("visibility", out var visibilityElement) ? visibilityElement.GetDouble() / 1000.0 : 0,
                        Description = weather.GetProperty("description").GetString() ?? "",
                        MainCondition = weather.GetProperty("main").GetString() ?? "",
                        Icon = weather.GetProperty("icon").GetString() ?? "",
                        WindSpeed = wind.GetProperty("speed").GetDouble(),
                        WindDirection = wind.TryGetProperty("deg", out var degElement) ? degElement.GetDouble() : 0,
                        Cloudiness = clouds.ValueKind != JsonValueKind.Undefined && clouds.TryGetProperty("all", out var cloudElement) ? cloudElement.GetDouble() : 0,
                        Latitude = coord.ValueKind != JsonValueKind.Undefined && coord.TryGetProperty("lat", out var latElement) ? latElement.GetDouble() : null,
                        Longitude = coord.ValueKind != JsonValueKind.Undefined && coord.TryGetProperty("lon", out var lonElement) ? lonElement.GetDouble() : null,
                        Timestamp = DateTime.UtcNow
                    };

                    // Save to MongoDB and track search
                    await _mongoDbService.SaveWeatherDataAsync(weatherData);
                    await _mongoDbService.IncrementCitySearchAsync(normalizedCity);
                    await _mongoDbService.IncrementCitySearchAsync(normalizedCity);
                    
                    return weatherData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching weather data: {ex.Message}");
            }
            
            return null;
        }

        public async Task<WeatherData?> GetWeatherByCoordinatesAsync(double latitude, double longitude)
        {
            try
            {
                var apiKey = _configuration["WeatherApi:Key"];
                var url = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={apiKey}&units=metric";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;
                    var main = root.GetProperty("main");
                    var weather = root.GetProperty("weather")[0];
                    var wind = root.GetProperty("wind");
                    var coord = root.TryGetProperty("coord", out var coordElement) ? coordElement : default;
                    var clouds = root.TryGetProperty("clouds", out var cloudsElement) ? cloudsElement : default;
                    
                    var cityName = root.GetProperty("name").GetString() ?? "";
                    Console.WriteLine($"[WeatherService] OpenWeatherMap returned city name: '{cityName}' for coordinates ({latitude}, {longitude})");
                    
                    // If OpenWeatherMap returns a landmark (like "Kanija Bhavan"), replace with city name
                    // For Bangalore area coordinates, default to "Bangalore"
                    string betterCityName = cityName;
                    if (latitude >= 12.5 && latitude <= 13.5 && longitude >= 77.0 && longitude <= 78.0)
                    {
                        // Coordinates are in Bangalore area
                        var cityLower = cityName.ToLower();
                        if (cityLower.Contains("bhavan") || cityLower.Contains("layout") || 
                            cityLower.Contains("nagar") || cityLower.Contains("extension") ||
                            cityLower.Contains("colony") || cityLower.Contains("road") ||
                            cityLower.Contains("extension"))
                        {
                            betterCityName = "Bangalore";
                            Console.WriteLine($"[WeatherService] ✅ Replaced landmark '{cityName}' with 'Bangalore'");
                        }
                    }
                    
                    var normalizedCity = NormalizeCityName(betterCityName);
                    Console.WriteLine($"[WeatherService] Final city name: '{normalizedCity}'");
                    
                    string countryCode = "";
                    if (root.TryGetProperty("sys", out var sysElement))
                    {
                        if (sysElement.TryGetProperty("country", out var countryElement))
                        {
                            countryCode = countryElement.GetString() ?? "";
                        }
                    }
                    
                    Console.WriteLine($"[WeatherService] Coordinates ({latitude}, {longitude}) mapped to: {normalizedCity}, {countryCode}");
                    Console.WriteLine($"[WeatherService] Original OpenWeatherMap name: {cityName}");
                    
                    var weatherData = new WeatherData
                    {
                        City = normalizedCity,
                        Country = countryCode,
                        Temperature = main.GetProperty("temp").GetDouble(),
                        FeelsLike = main.TryGetProperty("feels_like", out var feelsLikeElement) ? feelsLikeElement.GetDouble() : main.GetProperty("temp").GetDouble(),
                        Humidity = main.GetProperty("humidity").GetDouble(),
                        Pressure = main.TryGetProperty("pressure", out var pressureElement) ? pressureElement.GetDouble() : 0,
                        Visibility = root.TryGetProperty("visibility", out var visibilityElement) ? visibilityElement.GetDouble() / 1000.0 : 0,
                        Description = weather.GetProperty("description").GetString() ?? "",
                        MainCondition = weather.GetProperty("main").GetString() ?? "",
                        Icon = weather.GetProperty("icon").GetString() ?? "",
                        WindSpeed = wind.GetProperty("speed").GetDouble(),
                        WindDirection = wind.TryGetProperty("deg", out var degElement) ? degElement.GetDouble() : 0,
                        Cloudiness = clouds.ValueKind != JsonValueKind.Undefined && clouds.TryGetProperty("all", out var cloudElement) ? cloudElement.GetDouble() : 0,
                        Latitude = latitude,
                        Longitude = longitude,
                        Timestamp = DateTime.UtcNow
                    };

                    // Save to MongoDB
                    await _mongoDbService.SaveWeatherDataAsync(weatherData);
                    await _mongoDbService.IncrementCitySearchAsync(normalizedCity);
                    
                    return weatherData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching weather data by coordinates: {ex.Message}");
            }
            
            return null;
        }

        public async Task<ForecastData?> GetForecastAsync(string city, string? country = null)
        {
            try
            {
                var apiKey = _configuration["WeatherApi:Key"];
                var trimmedCity = city.Trim();
                var normalizedCity = NormalizeCityName(trimmedCity);
                var location = string.IsNullOrWhiteSpace(country)
                    ? normalizedCity
                    : $"{normalizedCity},{country.Trim()}";
                var encodedLocation = Uri.EscapeDataString(location);
                var url = $"https://api.openweathermap.org/data/2.5/forecast?q={encodedLocation}&appid={apiKey}&units=metric";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;
                    
                    var cityName = root.GetProperty("city").GetProperty("name").GetString() ?? normalizedCity;
                    var countryCode = root.GetProperty("city").TryGetProperty("country", out var countryElement) ? countryElement.GetString() ?? "" : (country ?? "");
                    var list = root.GetProperty("list");
                    
                    var forecastItems = new List<ForecastItem>();
                    
                    foreach (var item in list.EnumerateArray())
                    {
                        var main = item.GetProperty("main");
                        var weather = item.GetProperty("weather")[0];
                        var wind = item.GetProperty("wind");
                        var clouds = item.TryGetProperty("clouds", out var cloudsElement) ? cloudsElement : default;
                        var rain = item.TryGetProperty("rain", out var rainElement) ? rainElement : default;
                        var snow = item.TryGetProperty("snow", out var snowElement) ? snowElement : default;
                        
                        var forecastItem = new ForecastItem
                        {
                            DateTime = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).DateTime,
                            Temperature = main.GetProperty("temp").GetDouble(),
                            FeelsLike = main.TryGetProperty("feels_like", out var feelsLikeElement) ? feelsLikeElement.GetDouble() : main.GetProperty("temp").GetDouble(),
                            MinTemperature = main.GetProperty("temp_min").GetDouble(),
                            MaxTemperature = main.GetProperty("temp_max").GetDouble(),
                            Humidity = main.GetProperty("humidity").GetDouble(),
                            Pressure = main.TryGetProperty("pressure", out var pressureElement) ? pressureElement.GetDouble() : 0,
                            Description = weather.GetProperty("description").GetString() ?? "",
                            MainCondition = weather.GetProperty("main").GetString() ?? "",
                            Icon = weather.GetProperty("icon").GetString() ?? "",
                            WindSpeed = wind.GetProperty("speed").GetDouble(),
                            WindDirection = wind.TryGetProperty("deg", out var degElement) ? degElement.GetDouble() : 0,
                            Cloudiness = clouds.ValueKind != JsonValueKind.Undefined && clouds.TryGetProperty("all", out var cloudElement) ? cloudElement.GetDouble() : 0,
                            Precipitation = rain.ValueKind != JsonValueKind.Undefined && rain.TryGetProperty("3h", out var rain3h) 
                                ? rain3h.GetDouble() 
                                : (snow.ValueKind != JsonValueKind.Undefined && snow.TryGetProperty("3h", out var snow3h) ? snow3h.GetDouble() : null)
                        };
                        
                        forecastItems.Add(forecastItem);
                    }
                    
                    return new ForecastData
                    {
                        City = NormalizeCityName(cityName),
                        Country = countryCode,
                        Items = forecastItems
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching forecast data: {ex.Message}");
            }
            
            return null;
        }

        public async Task<ForecastData?> GetForecastByCoordinatesAsync(double latitude, double longitude)
        {
            try
            {
                var apiKey = _configuration["WeatherApi:Key"];
                var url = $"https://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&appid={apiKey}&units=metric";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;
                    
                    var cityName = root.GetProperty("city").GetProperty("name").GetString() ?? "";
                    var countryCode = root.GetProperty("city").TryGetProperty("country", out var countryElement) ? countryElement.GetString() ?? "" : "";
                    var list = root.GetProperty("list");
                    
                    var forecastItems = new List<ForecastItem>();
                    
                    foreach (var item in list.EnumerateArray())
                    {
                        var main = item.GetProperty("main");
                        var weather = item.GetProperty("weather")[0];
                        var wind = item.GetProperty("wind");
                        var clouds = item.TryGetProperty("clouds", out var cloudsElement) ? cloudsElement : default;
                        var rain = item.TryGetProperty("rain", out var rainElement) ? rainElement : default;
                        var snow = item.TryGetProperty("snow", out var snowElement) ? snowElement : default;
                        
                        var forecastItem = new ForecastItem
                        {
                            DateTime = DateTimeOffset.FromUnixTimeSeconds(item.GetProperty("dt").GetInt64()).DateTime,
                            Temperature = main.GetProperty("temp").GetDouble(),
                            FeelsLike = main.TryGetProperty("feels_like", out var feelsLikeElement) ? feelsLikeElement.GetDouble() : main.GetProperty("temp").GetDouble(),
                            MinTemperature = main.GetProperty("temp_min").GetDouble(),
                            MaxTemperature = main.GetProperty("temp_max").GetDouble(),
                            Humidity = main.GetProperty("humidity").GetDouble(),
                            Pressure = main.TryGetProperty("pressure", out var pressureElement) ? pressureElement.GetDouble() : 0,
                            Description = weather.GetProperty("description").GetString() ?? "",
                            MainCondition = weather.GetProperty("main").GetString() ?? "",
                            Icon = weather.GetProperty("icon").GetString() ?? "",
                            WindSpeed = wind.GetProperty("speed").GetDouble(),
                            WindDirection = wind.TryGetProperty("deg", out var degElement) ? degElement.GetDouble() : 0,
                            Cloudiness = clouds.ValueKind != JsonValueKind.Undefined && clouds.TryGetProperty("all", out var cloudElement) ? cloudElement.GetDouble() : 0,
                            Precipitation = rain.ValueKind != JsonValueKind.Undefined && rain.TryGetProperty("3h", out var rain3h) 
                                ? rain3h.GetDouble() 
                                : (snow.ValueKind != JsonValueKind.Undefined && snow.TryGetProperty("3h", out var snow3h) ? snow3h.GetDouble() : null)
                        };
                        
                        forecastItems.Add(forecastItem);
                    }
                    
                    return new ForecastData
                    {
                        City = NormalizeCityName(cityName),
                        Country = countryCode,
                        Items = forecastItems
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching forecast data by coordinates: {ex.Message}");
            }
            
            return null;
        }

        public async Task<List<string>> GetPopularCitiesAsync(int limit = 5)
        {
            var stats = await _mongoDbService.GetTopCitySearchesAsync(limit * 2);
            var ordered = stats
                .GroupBy(s => s.City, StringComparer.OrdinalIgnoreCase)
                .Select(g => g
                    .OrderByDescending(x => x.Count)
                    .ThenByDescending(x => x.LastSearched)
                    .First())
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.LastSearched)
                .Take(limit)
                .Select(x => x.City)
                .ToList();

            return ordered;
        }

        private async Task<string> GetCityNameFromCoordinatesAsync(double latitude, double longitude, string fallbackName)
        {
            try
            {
                // Use OpenWeatherMap reverse geocoding API to get better location name
                var apiKey = _configuration["WeatherApi:Key"];
                var url = $"https://api.openweathermap.org/geo/1.0/reverse?lat={latitude}&lon={longitude}&limit=1&appid={apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;
                    
                    if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                    {
                        var firstResult = root[0];
                        
                        // Try to get city name, fallback to name, then locality
                        string? cityName = null;
                        if (firstResult.TryGetProperty("name", out var nameElement))
                        {
                            cityName = nameElement.GetString();
                        }
                        
                        // Check if it's a landmark (like "Kanija Bhavan") and try to get the city instead
                        if (!string.IsNullOrEmpty(cityName) && 
                            (cityName.Contains("Bhavan") || cityName.Contains("Layout") || cityName.Contains("Nagar")))
                        {
                            // Try to get the state or district name as city
                            if (firstResult.TryGetProperty("state", out var stateElement))
                            {
                                var stateName = stateElement.GetString();
                                // If state is Karnataka, try to get district or use "Bangalore"
                                if (stateName?.Contains("Karnataka") == true)
                                {
                                    // Check if there's a better name in the response
                                    if (firstResult.TryGetProperty("local_names", out var localNames))
                                    {
                                        if (localNames.TryGetProperty("en", out var enName))
                                        {
                                            var enNameStr = enName.GetString();
                                            if (!string.IsNullOrEmpty(enNameStr) && !enNameStr.Contains("Bhavan"))
                                            {
                                                return enNameStr;
                                            }
                                        }
                                    }
                                    // Default to Bangalore for Karnataka coordinates
                                    return "Bangalore";
                                }
                            }
                        }
                        
                        // If we got a good city name, use it
                        if (!string.IsNullOrEmpty(cityName))
                        {
                            return cityName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in reverse geocoding: {ex.Message}");
            }
            
            // Fallback to original name
            return fallbackName;
        }

        private static string NormalizeCityName(string city)
        {
            var trimmed = city.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return trimmed;
            }

            var lower = trimmed.ToLowerInvariant();
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lower);
        }
    }
}

