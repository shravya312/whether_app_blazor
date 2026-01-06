using System.Text.RegularExpressions;
using System.Xml.Linq;
using WeatherApp.API.Models;

namespace WeatherApp.API.Services
{
    public class AIMLService
    {
        private readonly WeatherService _weatherService;
        private readonly MLService _mlService;
        private readonly ILogger<AIMLService> _logger;
        private readonly Dictionary<string, List<AIMLPattern>> _patterns;
        private string? _lastCity;

        public AIMLService(WeatherService weatherService, MLService mlService, ILogger<AIMLService> logger)
        {
            _weatherService = weatherService;
            _mlService = mlService;
            _logger = logger;
            _patterns = new Dictionary<string, List<AIMLPattern>>();
            LoadAIMLPatterns();
        }

        public async Task<string> ProcessQueryAsync(string userInput, string? userId = null)
        {
            try
            {
                var normalizedInput = NormalizeInput(userInput);
                _logger.LogInformation($"Processing query: {userInput} -> {normalizedInput}");

                // Use ML model to classify intent
                var intent = _mlService.ClassifyIntent(userInput);
                _logger.LogInformation($"ML Classified Intent: {intent}");

                // Use ML-based city extraction
                var city = _mlService.ExtractCityWithML(userInput);
                if (string.IsNullOrEmpty(city))
                {
                    city = ExtractCity(normalizedInput);
                }
                
                if (!string.IsNullOrEmpty(city))
                {
                    _lastCity = city;
                    _logger.LogInformation($"ML Extracted City: {city}");
                }

                // Process based on ML-classified intent
                switch (intent.ToLower())
                {
                    case "weather":
                        return await HandleWeatherQuery(city ?? _lastCity);
                    
                    case "temperature":
                        return await HandleTemperatureQuery(city ?? _lastCity);
                    
                    case "humidity":
                        return await HandleHumidityQuery(city ?? _lastCity);
                    
                    case "wind":
                        return await HandleWindQuery(city ?? _lastCity);
                    
                    case "pressure":
                        return await HandlePressureQuery(city ?? _lastCity);
                    
                    case "visibility":
                        return await HandleVisibilityQuery(city ?? _lastCity);
                    
                    case "cloudiness":
                        return await HandleCloudinessQuery(city ?? _lastCity);
                    
                    case "feelslike":
                        return await HandleFeelsLikeQuery(city ?? _lastCity);
                    
                    case "condition":
                        return await HandleConditionQuery(city ?? _lastCity);
                    
                    case "precipitation":
                        return await HandlePrecipitationQuery(city ?? _lastCity);
                    
                    case "forecast":
                        return await HandleForecastQuery(city ?? _lastCity);
                    
                    case "greeting":
                        return GetGreetingResponse();
                    
                    case "help":
                        return GetHelpResponse();
                    
                    default:
                        // Fallback to rule-based if ML didn't classify
                        return await ProcessQueryFallback(normalizedInput, city);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AIML query");
                return "I'm sorry, I encountered an error. Please try again.";
            }
        }

        private async Task<string> HandleWeatherQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city. For example: 'What's the weather in New York?'";
            }

            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                return FormatWeatherResponse(weather);
            }
            return $"Sorry, I couldn't find weather information for {city}. Please try another city.";
        }

        private async Task<string> HandleTemperatureQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city or ask about weather first.";
            }

            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                return $"The temperature in {city} is {weather.Temperature:F1}°C (feels like {weather.FeelsLike:F1}°C).";
            }
            return $"Sorry, I couldn't find temperature information for {city}.";
        }

        private async Task<string> HandleHumidityQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city. For example: 'What's the humidity in Bangalore?'";
            }

            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                return $"The humidity in {city} is {weather.Humidity:F0}%.";
            }
            return $"Sorry, I couldn't find humidity information for {city}. Please try another city.";
        }

        private async Task<string> HandleWindQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city. For example: 'What's the wind speed in Bangalore?'";
            }

            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                var direction = GetWindDirection(weather.WindDirection);
                return $"The wind speed in {city} is {weather.WindSpeed:F1} m/s ({weather.WindSpeed * 3.6:F1} km/h) from the {direction}.";
            }
            return $"Sorry, I couldn't find wind information for {city}. Please try another city.";
        }

        private async Task<string> HandlePressureQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city. For example: 'What's the pressure in Bangalore?'";
            }

            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                return $"The atmospheric pressure in {city} is {weather.Pressure:F0} hPa.";
            }
            return $"Sorry, I couldn't find pressure information for {city}. Please try another city.";
        }

        private async Task<string> HandleVisibilityQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city. For example: 'What's the visibility in Bangalore?'";
            }

            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                if (weather.Visibility > 0)
                {
                    return $"The visibility in {city} is {weather.Visibility:F1} km.";
                }
                return $"Visibility data is not available for {city}.";
            }
            return $"Sorry, I couldn't find visibility information for {city}. Please try another city.";
        }

        private async Task<string> HandleCloudinessQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city. For example: 'How cloudy is Bangalore?'";
            }

            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                var cloudinessDesc = GetCloudinessDescription(weather.Cloudiness);
                return $"The cloudiness in {city} is {weather.Cloudiness:F0}% ({cloudinessDesc}).";
            }
            return $"Sorry, I couldn't find cloudiness information for {city}. Please try another city.";
        }

        private async Task<string> HandleFeelsLikeQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city. For example: 'What does it feel like in Bangalore?'";
            }

            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                return $"In {city}, it feels like {weather.FeelsLike:F1}°C (actual temperature is {weather.Temperature:F1}°C).";
            }
            return $"Sorry, I couldn't find feels like temperature information for {city}. Please try another city.";
        }

        private async Task<string> HandleConditionQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city. For example: 'What's the weather condition in Bangalore?'";
            }

            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                var conditionDesc = GetConditionDescription(weather.MainCondition, weather.Description);
                return $"The weather condition in {city} is {conditionDesc}.";
            }
            return $"Sorry, I couldn't find weather condition information for {city}. Please try another city.";
        }

        private async Task<string> HandlePrecipitationQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city. For example: 'What's the precipitation in Bangalore?'";
            }

            // Try to get forecast data for precipitation
            var forecast = await _weatherService.GetForecastAsync(city);
            if (forecast != null && forecast.Items.Any())
            {
                var nextItem = forecast.Items.FirstOrDefault(f => f.Precipitation.HasValue && f.Precipitation.Value > 0);
                if (nextItem != null)
                {
                    var precipType = nextItem.MainCondition.ToLower().Contains("snow") ? "snowfall" : "rainfall";
                    return $"The expected {precipType} in {city} is {nextItem.Precipitation:F1} mm in the next 3 hours.";
                }
                return $"No precipitation expected in {city} in the near future.";
            }

            // Fallback to current weather condition
            var weather = await _weatherService.GetWeatherAsync(city);
            if (weather != null)
            {
                var condition = weather.MainCondition.ToLower();
                if (condition.Contains("rain") || condition.Contains("drizzle"))
                {
                    return $"It's currently {weather.Description.ToLower()} in {city}. Check the forecast for precipitation amounts.";
                }
                else if (condition.Contains("snow"))
                {
                    return $"It's currently {weather.Description.ToLower()} in {city}. Check the forecast for snowfall amounts.";
                }
                return $"No precipitation expected in {city} currently. The condition is {weather.Description.ToLower()}.";
            }
            return $"Sorry, I couldn't find precipitation information for {city}. Please try another city.";
        }

        private string GetCloudinessDescription(double cloudiness)
        {
            if (cloudiness < 10) return "Clear sky";
            if (cloudiness < 25) return "Few clouds";
            if (cloudiness < 50) return "Scattered clouds";
            if (cloudiness < 75) return "Broken clouds";
            return "Overcast";
        }

        private string GetConditionDescription(string mainCondition, string description)
        {
            if (!string.IsNullOrEmpty(description))
            {
                return description;
            }
            
            return mainCondition.ToLower() switch
            {
                "clear" => "Clear sky",
                "clouds" => "Cloudy",
                "rain" => "Rainy",
                "drizzle" => "Drizzling",
                "thunderstorm" => "Thunderstorm",
                "snow" => "Snowy",
                "mist" => "Misty",
                "fog" => "Foggy",
                "haze" => "Hazy",
                "dust" => "Dusty",
                "sand" => "Sandy",
                "ash" => "Ashy",
                "squall" => "Squally",
                "tornado" => "Tornado",
                _ => mainCondition
            };
        }

        private string GetWindDirection(double degrees)
        {
            var directions = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            var index = (int)Math.Round(degrees / 22.5) % 16;
            return directions[index];
        }

        private async Task<string> HandleForecastQuery(string? city)
        {
            if (string.IsNullOrEmpty(city))
            {
                return "Please specify a city for the forecast, or ask about weather first.";
            }

            var forecast = await _weatherService.GetForecastAsync(city);
            if (forecast != null && forecast.Items.Any())
            {
                return FormatForecastResponse(forecast);
            }
            return $"Sorry, I couldn't find forecast information for {city}.";
        }

        private async Task<string> ProcessQueryFallback(string normalizedInput, string? extractedCity)
        {
                // Check for weather queries
                if (IsWeatherQuery(normalizedInput))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput);
                    if (!string.IsNullOrEmpty(city))
                    {
                        _lastCity = city;
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null)
                        {
                            return FormatWeatherResponse(weather);
                        }
                        return $"Sorry, I couldn't find weather information for {city}. Please try another city.";
                    }
                }

                // Check for forecast queries
                if (IsForecastQuery(normalizedInput))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var forecast = await _weatherService.GetForecastAsync(city);
                        if (forecast != null && forecast.Items.Any())
                        {
                            return FormatForecastResponse(forecast);
                        }
                        return $"Sorry, I couldn't find forecast information for {city}.";
                    }
                    return "Please specify a city for the forecast, or ask about weather first.";
                }

                // Check for greeting
                if (IsGreeting(normalizedInput))
                {
                    return GetGreetingResponse();
                }

                // Check for help
                if (IsHelpQuery(normalizedInput))
                {
                    return GetHelpResponse();
                }

                // Check for humidity queries
                if (normalizedInput.Contains("humidity"))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null)
                        {
                            return $"The humidity in {city} is {weather.Humidity:F0}%.";
                        }
                        return $"Sorry, I couldn't find humidity information for {city}.";
                    }
                    return "Please specify a city. For example: 'Humidity in Bangalore'";
                }

                // Check for wind queries
                if (normalizedInput.Contains("wind"))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null)
                        {
                            var direction = GetWindDirection(weather.WindDirection);
                            return $"The wind speed in {city} is {weather.WindSpeed:F1} m/s ({weather.WindSpeed * 3.6:F1} km/h) from the {direction}.";
                        }
                        return $"Sorry, I couldn't find wind information for {city}.";
                    }
                    return "Please specify a city. For example: 'Wind speed in Bangalore'";
                }

                // Check for pressure queries
                if (normalizedInput.Contains("pressure") || normalizedInput.Contains("atmospheric"))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null)
                        {
                            return $"The atmospheric pressure in {city} is {weather.Pressure:F0} hPa.";
                        }
                        return $"Sorry, I couldn't find pressure information for {city}.";
                    }
                    return "Please specify a city. For example: 'Pressure in Bangalore'";
                }

                // Check for visibility queries
                if (normalizedInput.Contains("visibility") || normalizedInput.Contains("how far can i see"))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null && weather.Visibility > 0)
                        {
                            return $"The visibility in {city} is {weather.Visibility:F1} km.";
                        }
                        return $"Visibility data is not available for {city}.";
                    }
                    return "Please specify a city. For example: 'Visibility in Bangalore'";
                }

                // Check for cloudiness queries
                if (normalizedInput.Contains("cloudiness") || normalizedInput.Contains("cloudy") || normalizedInput.Contains("cloud cover") || normalizedInput.Contains("clouds"))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null)
                        {
                            var cloudinessDesc = GetCloudinessDescription(weather.Cloudiness);
                            return $"The cloudiness in {city} is {weather.Cloudiness:F0}% ({cloudinessDesc}).";
                        }
                        return $"Sorry, I couldn't find cloudiness information for {city}.";
                    }
                    return "Please specify a city. For example: 'Cloudiness in Bangalore'";
                }

                // Check for feels like queries
                if (normalizedInput.Contains("feels like") || normalizedInput.Contains("apparent temperature") || normalizedInput.Contains("what does it feel"))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null)
                        {
                            return $"In {city}, it feels like {weather.FeelsLike:F1}°C (actual temperature is {weather.Temperature:F1}°C).";
                        }
                        return $"Sorry, I couldn't find feels like temperature information for {city}.";
                    }
                    return "Please specify a city. For example: 'Feels like in Bangalore'";
                }

                // Check for condition queries
                if (normalizedInput.Contains("condition") || normalizedInput.Contains("is it raining") || normalizedInput.Contains("is it sunny") || normalizedInput.Contains("description"))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null)
                        {
                            var conditionDesc = GetConditionDescription(weather.MainCondition, weather.Description);
                            return $"The weather condition in {city} is {conditionDesc}.";
                        }
                        return $"Sorry, I couldn't find weather condition information for {city}.";
                    }
                    return "Please specify a city. For example: 'Weather condition in Bangalore'";
                }

                // Check for precipitation queries
                if (normalizedInput.Contains("precipitation") || normalizedInput.Contains("rainfall") || normalizedInput.Contains("snowfall") || 
                    (normalizedInput.Contains("rain") && !normalizedInput.Contains("raining")) || normalizedInput.Contains("snow"))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var forecast = await _weatherService.GetForecastAsync(city);
                        if (forecast != null && forecast.Items.Any())
                        {
                            var nextItem = forecast.Items.FirstOrDefault(f => f.Precipitation.HasValue && f.Precipitation.Value > 0);
                            if (nextItem != null)
                            {
                                var precipType = nextItem.MainCondition.ToLower().Contains("snow") ? "snowfall" : "rainfall";
                                return $"The expected {precipType} in {city} is {nextItem.Precipitation:F1} mm in the next 3 hours.";
                            }
                            return $"No precipitation expected in {city} in the near future.";
                        }
                        
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null)
                        {
                            var condition = weather.MainCondition.ToLower();
                            if (condition.Contains("rain") || condition.Contains("drizzle"))
                            {
                                return $"It's currently {weather.Description.ToLower()} in {city}. Check the forecast for precipitation amounts.";
                            }
                            else if (condition.Contains("snow"))
                            {
                                return $"It's currently {weather.Description.ToLower()} in {city}. Check the forecast for snowfall amounts.";
                            }
                            return $"No precipitation expected in {city} currently. The condition is {weather.Description.ToLower()}.";
                        }
                        return $"Sorry, I couldn't find precipitation information for {city}.";
                    }
                    return "Please specify a city. For example: 'Precipitation in Bangalore'";
                }

                // Check for temperature queries
                if (IsTemperatureQuery(normalizedInput))
                {
                    var city = extractedCity ?? ExtractCity(normalizedInput) ?? _lastCity;
                    if (!string.IsNullOrEmpty(city))
                    {
                        var weather = await _weatherService.GetWeatherAsync(city);
                        if (weather != null)
                        {
                            return $"The temperature in {city} is {weather.Temperature:F1}°C (feels like {weather.FeelsLike:F1}°C).";
                        }
                        return $"Sorry, I couldn't find temperature information for {city}.";
                    }
                    return "Please specify a city or ask about weather first.";
                }

                // Default response
                return GetDefaultResponse(normalizedInput);
        }

        private string NormalizeInput(string input)
        {
            return input.ToLower().Trim();
        }

        private bool IsWeatherQuery(string input)
        {
            var patterns = new[]
            {
                @"what.*weather.*in",
                @"weather.*in",
                @"how.*weather.*in",
                @"tell.*weather.*in",
                @"show.*weather.*in",
                @"weather.*for",
                @"what.*weather.*for"
            };

            return patterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        private bool IsForecastQuery(string input)
        {
            var patterns = new[]
            {
                @"forecast",
                @"prediction",
                @"future.*weather",
                @"weather.*tomorrow",
                @"weather.*next.*days",
                @"5.*day.*forecast"
            };

            return patterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        private bool IsTemperatureQuery(string input)
        {
            var patterns = new[]
            {
                @"temperature",
                @"temp",
                @"how.*hot",
                @"how.*cold",
                @"what.*temp"
            };

            return patterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
        }

        private bool IsGreeting(string input)
        {
            var greetings = new[]
            {
                "hello", "hi", "hey", "greetings", "good morning", "good afternoon",
                "good evening", "howdy", "sup", "what's up"
            };

            return greetings.Any(g => input.Contains(g));
        }

        private bool IsHelpQuery(string input)
        {
            var helpKeywords = new[]
            {
                "help", "what can you do", "how can you help", "what do you do",
                "commands", "features", "assist"
            };

            return helpKeywords.Any(k => input.Contains(k));
        }

        private string? ExtractCity(string input)
        {
            // Try to extract city name from common patterns
            var patterns = new[]
            {
                @"(?:weather|forecast|temperature|temp).*?(?:in|for|at)\s+([A-Z][a-zA-Z\s]+)",
                @"(?:in|for|at)\s+([A-Z][a-zA-Z\s]+)",
                @"([A-Z][a-zA-Z\s]+)(?:\s+weather|\s+forecast)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var city = match.Groups[1].Value.Trim();
                    // Remove common words
                    city = Regex.Replace(city, @"\b(in|for|at|the|a|an|is|are|what|how|tell|show)\b", "", RegexOptions.IgnoreCase).Trim();
                    if (!string.IsNullOrEmpty(city) && city.Length > 2)
                    {
                        return city;
                    }
                }
            }

            // If no pattern matches, try to find capitalized words (likely city names)
            var words = input.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (char.IsUpper(word[0]) && word.Length > 2 && !IsCommonWord(word))
                {
                    return word;
                }
            }

            return null;
        }

        private bool IsCommonWord(string word)
        {
            var commonWords = new[]
            {
                "What", "How", "Tell", "Show", "Weather", "Forecast", "Temperature",
                "The", "In", "For", "At", "Is", "Are", "Can", "You", "Me", "My"
            };

            return commonWords.Contains(word, StringComparer.OrdinalIgnoreCase);
        }

        private string FormatWeatherResponse(WeatherData weather)
        {
            return $"The weather in {weather.City}, {weather.Country} is {weather.Description.ToLower()} " +
                   $"with a temperature of {weather.Temperature:F1}°C (feels like {weather.FeelsLike:F1}°C). " +
                   $"Humidity: {weather.Humidity:F0}%, Wind Speed: {weather.WindSpeed:F1} m/s, " +
                   $"Pressure: {weather.Pressure:F0} hPa.";
        }

        private string FormatForecastResponse(ForecastData forecast)
        {
            var response = $"Here's the 5-day forecast for {forecast.City}, {forecast.Country}:\n\n";
            var grouped = forecast.Items.GroupBy(f => f.DateTime.Date).Take(5);
            
            foreach (var day in grouped)
            {
                var dayItem = day.First();
                var maxTemp = day.Max(f => f.MaxTemperature);
                var minTemp = day.Min(f => f.MinTemperature);
                var dateStr = dayItem.DateTime.ToString("dddd, MMM dd");
                response += $"{dateStr}: {dayItem.Description}, High: {maxTemp:F1}°C, Low: {minTemp:F1}°C\n";
            }

            return response;
        }

        private string GetGreetingResponse()
        {
            var greetings = new[]
            {
                "Hello! I'm your weather assistant. How can I help you today?",
                "Hi there! I can help you with weather information. What would you like to know?",
                "Hey! Ask me about weather in any city, and I'll help you out!",
                "Greetings! I'm here to help with weather queries. What city are you interested in?"
            };

            return greetings[new Random().Next(greetings.Length)];
        }

        private string GetHelpResponse()
        {
            return "I can help you with all weather parameters:\n" +
                   "• Weather information (e.g., 'What's the weather in New York?')\n" +
                   "• Temperature (e.g., 'Temperature in London')\n" +
                   "• Feels Like (e.g., 'Feels like in Tokyo')\n" +
                   "• Humidity (e.g., 'Humidity in Bangalore')\n" +
                   "• Wind Speed & Direction (e.g., 'Wind speed in Mumbai')\n" +
                   "• Pressure (e.g., 'Pressure in Delhi')\n" +
                   "• Visibility (e.g., 'Visibility in Sydney')\n" +
                   "• Cloudiness (e.g., 'Cloudiness in Paris')\n" +
                   "• Weather Condition (e.g., 'Is it raining in London?')\n" +
                   "• Precipitation (e.g., 'Rainfall in Tokyo')\n" +
                   "• 5-day Forecasts (e.g., 'Forecast for Bangalore')\n\n" +
                   "Just ask me naturally, and I'll provide the information!";
        }

        private string GetDefaultResponse(string input)
        {
            var responses = new[]
            {
                "I'm not sure I understand. Try asking about weather in a specific city, like 'What's the weather in New York?'",
                "Could you rephrase that? I can help with weather information for cities.",
                "I'm a weather assistant. Try asking 'What's the weather in [city name]?'",
                "I didn't catch that. You can ask me about weather, temperature, or forecasts for any city."
            };

            return responses[new Random().Next(responses.Length)];
        }

        private void LoadAIMLPatterns()
        {
            // Load AIML patterns from files if needed
            // For now, we're using pattern matching directly
            _logger.LogInformation("AIML patterns loaded (using pattern matching)");
        }

        private class AIMLPattern
        {
            public string Pattern { get; set; } = string.Empty;
            public string Template { get; set; } = string.Empty;
        }
    }
}

