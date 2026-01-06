using Microsoft.ML.Data;

namespace WeatherApp.API.Models
{
    // Input model for intent classification
    public class IntentInput
    {
        [LoadColumn(0)]
        public string Text { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string Intent { get; set; } = string.Empty;
    }

    // Output model for intent prediction
    public class IntentPrediction
    {
        [ColumnName("PredictedLabel")]
        public string Intent { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();
    }

    // Input model for city extraction
    public class CityExtractionInput
    {
        [LoadColumn(0)]
        public string Query { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string City { get; set; } = string.Empty;
    }

    // Output model for city prediction
    public class CityPrediction
    {
        [ColumnName("PredictedLabel")]
        public string City { get; set; } = string.Empty;

        [ColumnName("Score")]
        public float[] Score { get; set; } = Array.Empty<float>();
    }

    // Weather prediction input
    public class WeatherPredictionInput
    {
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Pressure { get; set; }
        public float WindSpeed { get; set; }
        public float Cloudiness { get; set; }
        public int Hour { get; set; }
        public int DayOfYear { get; set; }
    }

    // Weather prediction output
    public class WeatherPredictionOutput
    {
        [ColumnName("Score")]
        public float PredictedTemperature { get; set; }
    }
}

