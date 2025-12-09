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
                    
                    // Get coordinates from API response to verify city name
                    double? apiLatitude = null;
                    double? apiLongitude = null;
                    if (coord.ValueKind != JsonValueKind.Undefined)
                    {
                        if (coord.TryGetProperty("lat", out var latElement))
                            apiLatitude = latElement.GetDouble();
                        if (coord.TryGetProperty("lon", out var lonElement))
                            apiLongitude = lonElement.GetDouble();
                    }
                    
                    // Get actual city name returned by API (might be different from search query)
                    var apiCityName = root.TryGetProperty("name", out var nameElement) 
                        ? nameElement.GetString() ?? normalizedCity 
                        : normalizedCity;
                    
                    // Correct city name based on coordinates if available
                    string correctedCityName = normalizedCity;
                    if (apiLatitude.HasValue && apiLongitude.HasValue)
                    {
                        correctedCityName = CorrectCityNameByCoordinates(apiLatitude.Value, apiLongitude.Value, apiCityName);
                        Console.WriteLine($"[WeatherService] City search: '{city}' -> API returned '{apiCityName}' -> Corrected to '{correctedCityName}'");
                    }
                    else
                    {
                        // If no coordinates, use API's city name but check for common misidentifications
                        correctedCityName = apiCityName;
                    }
                    
                    // Normalize the corrected city name
                    correctedCityName = NormalizeCityName(correctedCityName);
                    
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
                        City = correctedCityName,
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
                        Latitude = apiLatitude,
                        Longitude = apiLongitude,
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
                    
                    // Correct city name based on coordinates and common misidentifications
                    string betterCityName = CorrectCityNameByCoordinates(latitude, longitude, cityName);
                    
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

        /// <summary>
        /// Corrects city name based on coordinate boundaries - if coordinates are within a city's boundaries, use that city
        /// Pure boundary-based logic: no distance calculations
        /// </summary>
        private static string CorrectCityNameByCoordinates(double latitude, double longitude, string apiCityName)
        {
            var cityLower = apiCityName.ToLower();
            
            // Define city boundaries (boundary-based only, no distance calculations)
            var cityBoundaries = new List<(string City, double MinLat, double MaxLat, double MinLon, double MaxLon, List<string> WrongNames, List<string> LandmarkKeywords)>
            {
                // Bangalore, India - City boundaries
                // Bangalore city limits: approximately 12.834°N to 13.139°N, 77.407°E to 77.704°E
                // Extended slightly to cover Greater Bangalore area: 12.80°N to 13.15°N, 77.35°E to 77.80°E
                ("Bangalore", 12.80, 13.15, 77.35, 77.80, 
                    new List<string> { "mysore", "mysuru", "tumakuru", "tumkur" },
                    new List<string> { "bhavan", "layout", "nagar", "extension", "colony", "road", "street", "avenue" }),
                
                // Mysore, India - City boundaries  
                // Mysore city limits: approximately 12.20°N to 12.40°N, 76.50°E to 76.75°E
                ("Mysore", 12.20, 12.40, 76.50, 76.75,
                    new List<string> { },
                    new List<string> { "bhavan", "layout", "nagar", "extension", "colony", "road", "street", "avenue" }),
                
                // Mumbai, India
                ("Mumbai", 18.5, 19.5, 72.5, 73.5,
                    new List<string> { "thane", "navi mumbai", "kalyan" },
                    new List<string> { "bhavan", "nagar", "colony", "road", "street" }),
                
                // Delhi, India
                ("Delhi", 28.3, 29.0, 76.8, 77.5,
                    new List<string> { "gurgaon", "gurugram", "noida", "ghaziabad", "faridabad" },
                    new List<string> { "nagar", "colony", "road", "street", "extension" }),
                
                // Chennai, India
                ("Chennai", 12.8, 13.3, 80.0, 80.4,
                    new List<string> { "madras" },
                    new List<string> { "nagar", "colony", "road", "street" }),
                
                // Hyderabad, India
                ("Hyderabad", 17.2, 17.6, 78.2, 78.7,
                    new List<string> { "secunderabad" },
                    new List<string> { "nagar", "colony", "road", "street" }),
                
                // Kolkata, India
                ("Kolkata", 22.4, 22.7, 88.2, 88.5,
                    new List<string> { "calcutta", "howrah" },
                    new List<string> { "nagar", "colony", "road", "street" }),
                
                // Pune, India
                ("Pune", 18.3, 18.7, 73.6, 74.0,
                    new List<string> { "pimpri", "chinchwad" },
                    new List<string> { "nagar", "colony", "road", "street" }),
                
                // New York, USA
                ("New York", 40.4, 40.9, -74.3, -73.7,
                    new List<string> { "newark", "jersey city", "yonkers" },
                    new List<string> { "avenue", "street", "boulevard", "park" }),
                
                // London, UK
                ("London", 51.3, 51.7, -0.5, 0.3,
                    new List<string> { "westminster", "greenwich", "camden" },
                    new List<string> { "street", "road", "avenue", "park" }),
                
                // Tokyo, Japan
                ("Tokyo", 35.4, 35.9, 139.4, 139.9,
                    new List<string> { "shibuya", "shinjuku", "chiyoda" },
                    new List<string> { "station", "street", "avenue" }),
                
                // Sydney, Australia
                ("Sydney", -34.0, -33.7, 150.9, 151.3,
                    new List<string> { "parramatta", "north sydney" },
                    new List<string> { "street", "road", "avenue", "park" }),
                
                // Dubai, UAE
                ("Dubai", 24.8, 25.4, 55.0, 55.5,
                    new List<string> { "sharjah", "ajman" },
                    new List<string> { "street", "road", "avenue" }),
            };
            
            // Check each city boundary - if coordinates are within boundaries, use that city
            foreach (var boundary in cityBoundaries)
            {
                // Simple boundary check: coordinates must be within lat/lon range
                if (latitude >= boundary.MinLat && latitude <= boundary.MaxLat &&
                    longitude >= boundary.MinLon && longitude <= boundary.MaxLon)
                {
                    // Coordinates are within this city's boundaries
                    var cityVariations = new List<string> 
                    { 
                        boundary.City.ToLower(),
                        boundary.City.Replace(" ", "").ToLower()
                    };
                    
                    // Add common variations
                    if (boundary.City == "Bangalore")
                    {
                        cityVariations.Add("bengaluru");
                    }
                    else if (boundary.City == "Mumbai")
                    {
                        cityVariations.Add("bombay");
                    }
                    else if (boundary.City == "Kolkata")
                    {
                        cityVariations.Add("calcutta");
                    }
                    else if (boundary.City == "Mysore")
                    {
                        cityVariations.Add("mysuru");
                    }
                    
                    // Boundary-based logic: If coordinates are in city boundaries, ALWAYS use that city
                    // Check if API returned wrong city name or landmark
                    bool apiMatchesCity = cityVariations.Any(variation => cityLower.Contains(variation));
                    bool apiReturnedWrongCity = boundary.WrongNames.Any(wrong => cityLower.Contains(wrong) || cityLower == wrong);
                    bool apiReturnedLandmark = boundary.LandmarkKeywords.Any(keyword => cityLower.Contains(keyword));
                    
                    // Always correct if API returned wrong city, landmark, or doesn't match
                    // This ensures boundary-based accuracy - if you're in Bangalore boundaries, show Bangalore
                    if (!apiMatchesCity || apiReturnedWrongCity || apiReturnedLandmark)
                    {
                        Console.WriteLine($"[WeatherService] ✅ Boundary-based correction: Coordinates ({latitude:F4}, {longitude:F4}) are in {boundary.City} boundaries");
                        Console.WriteLine($"[WeatherService] ✅ Corrected '{apiCityName}' to '{boundary.City}'");
                        return boundary.City;
                    }
                    
                    // Even if API matches, ensure we use the standardized city name from boundaries
                    // This ensures consistency (e.g., "Bengaluru" → "Bangalore")
                    if (boundary.City == "Bangalore" && (cityLower.Contains("bengaluru") || cityLower.Contains("bangalore")))
                    {
                        Console.WriteLine($"[WeatherService] ✅ Coordinates ({latitude:F4}, {longitude:F4}) are in {boundary.City} boundaries - standardizing to '{boundary.City}'");
                        return boundary.City;
                    }
                    
                    // For other cities, if API matches correctly, use boundary city name for consistency
                    Console.WriteLine($"[WeatherService] ✅ Coordinates ({latitude:F4}, {longitude:F4}) are in {boundary.City} boundaries - using '{boundary.City}'");
                    return boundary.City;
                }
            }
            
            // If coordinates don't match any known boundaries, return API's city name as-is
            Console.WriteLine($"[WeatherService] Coordinates ({latitude:F4}, {longitude:F4}) don't match any known city boundaries - using API result: '{apiCityName}'");
            return apiCityName;
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

