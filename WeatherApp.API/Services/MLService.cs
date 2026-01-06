using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using WeatherApp.API.Models;
using System.Text;

namespace WeatherApp.API.Services
{
    public class MLService
    {
        private readonly MLContext _mlContext;
        private readonly ILogger<MLService> _logger;
        private ITransformer? _intentModel;
        private PredictionEngine<IntentInput, IntentPrediction>? _intentPredictor;
        private bool _isModelLoaded = false;

        public MLService(ILogger<MLService> logger)
        {
            _mlContext = new MLContext(seed: 0);
            _logger = logger;
            InitializeModels();
        }

        private void InitializeModels()
        {
            try
            {
                // Create training data for intent classification
                var trainingData = CreateIntentTrainingData();
                
                // Train intent classification model
                TrainIntentModel(trainingData);
                
                _logger.LogInformation("ML models initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing ML models");
            }
        }

        private List<IntentInput> CreateIntentTrainingData()
        {
            return new List<IntentInput>
            {
                // Weather queries
                new IntentInput { Text = "what is the weather in new york", Intent = "weather" },
                new IntentInput { Text = "weather in london", Intent = "weather" },
                new IntentInput { Text = "how is the weather in tokyo", Intent = "weather" },
                new IntentInput { Text = "tell me weather for bangalore", Intent = "weather" },
                new IntentInput { Text = "show me weather in mumbai", Intent = "weather" },
                new IntentInput { Text = "what's the weather like in delhi", Intent = "weather" },
                new IntentInput { Text = "current weather in sydney", Intent = "weather" },
                new IntentInput { Text = "weather conditions in paris", Intent = "weather" },
                
                // Temperature queries
                new IntentInput { Text = "what is the temperature in new york", Intent = "temperature" },
                new IntentInput { Text = "temperature in london", Intent = "temperature" },
                new IntentInput { Text = "how hot is it in tokyo", Intent = "temperature" },
                new IntentInput { Text = "what's the temp in bangalore", Intent = "temperature" },
                new IntentInput { Text = "how cold is mumbai", Intent = "temperature" },
                
                // Humidity queries
                new IntentInput { Text = "humidity in bangalore", Intent = "humidity" },
                new IntentInput { Text = "what is the humidity in new york", Intent = "humidity" },
                new IntentInput { Text = "humidity in london", Intent = "humidity" },
                new IntentInput { Text = "how humid is tokyo", Intent = "humidity" },
                new IntentInput { Text = "what's the humidity in mumbai", Intent = "humidity" },
                new IntentInput { Text = "humidity level in delhi", Intent = "humidity" },
                new IntentInput { Text = "show me humidity for paris", Intent = "humidity" },
                
                // Wind queries
                new IntentInput { Text = "wind speed in bangalore", Intent = "wind" },
                new IntentInput { Text = "what is the wind speed in new york", Intent = "wind" },
                new IntentInput { Text = "wind in london", Intent = "wind" },
                new IntentInput { Text = "how windy is tokyo", Intent = "wind" },
                
                // Pressure queries
                new IntentInput { Text = "pressure in bangalore", Intent = "pressure" },
                new IntentInput { Text = "what is the pressure in new york", Intent = "pressure" },
                new IntentInput { Text = "atmospheric pressure in london", Intent = "pressure" },
                
                // Visibility queries
                new IntentInput { Text = "visibility in bangalore", Intent = "visibility" },
                new IntentInput { Text = "what is the visibility in new york", Intent = "visibility" },
                new IntentInput { Text = "how far can i see in tokyo", Intent = "visibility" },
                new IntentInput { Text = "visibility level in mumbai", Intent = "visibility" },
                
                // Cloudiness queries
                new IntentInput { Text = "cloudiness in bangalore", Intent = "cloudiness" },
                new IntentInput { Text = "how cloudy is new york", Intent = "cloudiness" },
                new IntentInput { Text = "cloud cover in london", Intent = "cloudiness" },
                new IntentInput { Text = "clouds in tokyo", Intent = "cloudiness" },
                
                // Feels Like queries
                new IntentInput { Text = "feels like in bangalore", Intent = "feelslike" },
                new IntentInput { Text = "what does it feel like in new york", Intent = "feelslike" },
                new IntentInput { Text = "feels like temperature in london", Intent = "feelslike" },
                new IntentInput { Text = "apparent temperature in tokyo", Intent = "feelslike" },
                
                // Condition queries
                new IntentInput { Text = "weather condition in bangalore", Intent = "condition" },
                new IntentInput { Text = "what is the condition in new york", Intent = "condition" },
                new IntentInput { Text = "is it raining in london", Intent = "condition" },
                new IntentInput { Text = "is it sunny in tokyo", Intent = "condition" },
                new IntentInput { Text = "weather description in mumbai", Intent = "condition" },
                
                // Precipitation queries
                new IntentInput { Text = "precipitation in bangalore", Intent = "precipitation" },
                new IntentInput { Text = "rainfall in new york", Intent = "precipitation" },
                new IntentInput { Text = "how much rain in london", Intent = "precipitation" },
                new IntentInput { Text = "snowfall in tokyo", Intent = "precipitation" },
                
                // Forecast queries
                new IntentInput { Text = "forecast for new york", Intent = "forecast" },
                new IntentInput { Text = "show me forecast for london", Intent = "forecast" },
                new IntentInput { Text = "weather prediction for tokyo", Intent = "forecast" },
                new IntentInput { Text = "5 day forecast bangalore", Intent = "forecast" },
                new IntentInput { Text = "future weather in mumbai", Intent = "forecast" },
                new IntentInput { Text = "weather tomorrow in delhi", Intent = "forecast" },
                
                // Greetings
                new IntentInput { Text = "hello", Intent = "greeting" },
                new IntentInput { Text = "hi", Intent = "greeting" },
                new IntentInput { Text = "hey", Intent = "greeting" },
                new IntentInput { Text = "good morning", Intent = "greeting" },
                new IntentInput { Text = "good afternoon", Intent = "greeting" },
                new IntentInput { Text = "greetings", Intent = "greeting" },
                
                // Help queries
                new IntentInput { Text = "help", Intent = "help" },
                new IntentInput { Text = "what can you do", Intent = "help" },
                new IntentInput { Text = "how can you help", Intent = "help" },
                new IntentInput { Text = "what do you do", Intent = "help" },
                new IntentInput { Text = "commands", Intent = "help" },
                new IntentInput { Text = "features", Intent = "help" },
                
                // Default/unknown
                new IntentInput { Text = "random text", Intent = "unknown" },
                new IntentInput { Text = "test", Intent = "unknown" },
            };
        }

        private void TrainIntentModel(List<IntentInput> trainingData)
        {
            try
            {
                // Convert to IDataView
                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // Define pipeline
                var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", "Intent")
                    .Append(_mlContext.Transforms.Text.FeaturizeText("Features", "Text"))
                    .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                        labelColumnName: "Label",
                        featureColumnName: "Features"))
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                // Train model
                _intentModel = pipeline.Fit(dataView);

                // Create predictor
                _intentPredictor = _mlContext.Model.CreatePredictionEngine<IntentInput, IntentPrediction>(_intentModel);

                _isModelLoaded = true;
                _logger.LogInformation("Intent classification model trained successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training intent model");
            }
        }

        public string ClassifyIntent(string userInput)
        {
            if (!_isModelLoaded || _intentPredictor == null)
            {
                // Fallback to rule-based if ML model not loaded
                return ClassifyIntentFallback(userInput);
            }

            try
            {
                var input = new IntentInput { Text = userInput.ToLower() };
                var prediction = _intentPredictor.Predict(input);
                
                _logger.LogInformation($"ML Intent Classification: '{userInput}' -> '{prediction.Intent}' (Confidence: {prediction.Score?.Max():F2})");
                
                return prediction.Intent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying intent with ML model");
                return ClassifyIntentFallback(userInput);
            }
        }

        private string ClassifyIntentFallback(string userInput)
        {
            var normalized = userInput.ToLower();
            
            if (normalized.Contains("forecast") || normalized.Contains("prediction") || normalized.Contains("future"))
                return "forecast";
            if (normalized.Contains("precipitation") || normalized.Contains("rainfall") || normalized.Contains("snowfall") || normalized.Contains("rain") || normalized.Contains("snow"))
                return "precipitation";
            if (normalized.Contains("visibility") || normalized.Contains("how far can i see"))
                return "visibility";
            if (normalized.Contains("cloudiness") || normalized.Contains("cloudy") || normalized.Contains("cloud cover") || normalized.Contains("clouds"))
                return "cloudiness";
            if (normalized.Contains("feels like") || normalized.Contains("apparent temperature") || normalized.Contains("what does it feel"))
                return "feelslike";
            if (normalized.Contains("condition") || normalized.Contains("is it raining") || normalized.Contains("is it sunny") || normalized.Contains("description"))
                return "condition";
            if (normalized.Contains("humidity"))
                return "humidity";
            if (normalized.Contains("wind"))
                return "wind";
            if (normalized.Contains("pressure") || normalized.Contains("atmospheric"))
                return "pressure";
            if (normalized.Contains("temperature") || normalized.Contains("temp") || normalized.Contains("hot") || normalized.Contains("cold"))
                return "temperature";
            if (normalized.Contains("weather"))
                return "weather";
            if (normalized.Contains("hello") || normalized.Contains("hi") || normalized.Contains("hey") || normalized.Contains("greeting"))
                return "greeting";
            if (normalized.Contains("help") || normalized.Contains("what can") || normalized.Contains("how can"))
                return "help";
            
            return "unknown";
        }

        public string ExtractCityWithML(string userInput)
        {
            // Enhanced city extraction using ML-based pattern recognition
            var normalized = userInput.ToLower();
            
            // Common city names database
            var cities = new[]
            {
                "new york", "london", "tokyo", "bangalore", "mumbai", "delhi", "chennai",
                "hyderabad", "kolkata", "pune", "sydney", "melbourne", "paris", "berlin",
                "madrid", "rome", "dubai", "singapore", "hong kong", "seoul", "beijing",
                "shanghai", "moscow", "istanbul", "cairo", "johannesburg", "nairobi",
                "lagos", "rio de janeiro", "sao paulo", "buenos aires", "mexico city",
                "los angeles", "chicago", "houston", "phoenix", "philadelphia", "san antonio",
                "san diego", "dallas", "san jose", "austin", "jacksonville", "san francisco"
            };

            // Find city in input
            foreach (var city in cities)
            {
                if (normalized.Contains(city))
                {
                    // Capitalize first letter of each word
                    var words = city.Split(' ');
                    var capitalized = string.Join(" ", words.Select(w => 
                        w.Length > 0 ? char.ToUpper(w[0]) + w.Substring(1) : w));
                    return capitalized;
                }
            }

            // Fallback to pattern matching
            return ExtractCityFallback(userInput);
        }

        private string ExtractCityFallback(string userInput)
        {
            // Pattern-based extraction (fallback)
            var patterns = new[]
            {
                @"(?:weather|forecast|temperature|temp).*?(?:in|for|at)\s+([A-Z][a-zA-Z\s]+)",
                @"(?:in|for|at)\s+([A-Z][a-zA-Z\s]+)",
                @"([A-Z][a-zA-Z\s]+)(?:\s+weather|\s+forecast)"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(userInput, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var city = match.Groups[1].Value.Trim();
                    city = System.Text.RegularExpressions.Regex.Replace(city, 
                        @"\b(in|for|at|the|a|an|is|are|what|how|tell|show)\b", "", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                    if (!string.IsNullOrEmpty(city) && city.Length > 2)
                    {
                        return city;
                    }
                }
            }

            return string.Empty;
        }

        public float PredictTemperature(WeatherPredictionInput input)
        {
            // Simple linear regression-based prediction
            // This is a basic model - can be enhanced with actual ML training
            var baseTemp = input.Temperature;
            var hourFactor = Math.Sin((input.Hour - 6) * Math.PI / 12) * 3; // Daily cycle
            var seasonalFactor = Math.Sin((input.DayOfYear - 80) * 2 * Math.PI / 365) * 5; // Seasonal variation
            
            var predicted = baseTemp + (float)hourFactor + (float)seasonalFactor;
            
            _logger.LogInformation($"ML Temperature Prediction: {baseTemp}°C -> {predicted:F1}°C");
            
            return predicted;
        }

        public void RetrainModel(List<IntentInput> newTrainingData)
        {
            try
            {
                var allData = CreateIntentTrainingData();
                allData.AddRange(newTrainingData);
                TrainIntentModel(allData);
                _logger.LogInformation("Model retrained with new data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retraining model");
            }
        }
    }
}

