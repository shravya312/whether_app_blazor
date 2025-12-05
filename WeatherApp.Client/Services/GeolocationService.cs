using Microsoft.JSInterop;

namespace WeatherApp.Client.Services
{
    public class GeolocationService
    {
        private readonly IJSRuntime _jsRuntime;

        public GeolocationService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<(double Latitude, double Longitude, string? Error)?> GetCurrentLocationAsync()
        {
            try
            {
                Console.WriteLine("[GeolocationService] Attempting to get browser geolocation...");
                var result = await _jsRuntime.InvokeAsync<LocationResult>("getCurrentLocation");
                if (result != null)
                {
                    if (result.Success)
                    {
                        Console.WriteLine($"[GeolocationService] ✅ Browser geolocation SUCCESS!");
                        Console.WriteLine($"[GeolocationService] Latitude: {result.Latitude}");
                        Console.WriteLine($"[GeolocationService] Longitude: {result.Longitude}");
                        Console.WriteLine($"[GeolocationService] Coordinates: ({result.Latitude}, {result.Longitude})");
                        return (result.Latitude, result.Longitude, null);
                    }
                    else
                    {
                        Console.WriteLine($"[GeolocationService] ❌ Browser geolocation FAILED: {result.Error}");
                        // Return error message with location data
                        return (0, 0, result.Error ?? "Unknown geolocation error");
                    }
                }
            }
            catch (JSException ex)
            {
                Console.WriteLine($"[GeolocationService] ❌ JS Error getting location: {ex.Message}");
                return (0, 0, $"JavaScript error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GeolocationService] ❌ Error getting location: {ex.Message}");
                return (0, 0, $"Error: {ex.Message}");
            }
            Console.WriteLine("[GeolocationService] ❌ Failed to get location - result was null");
            return (0, 0, "Failed to get location");
        }

        private class LocationResult
        {
            public bool Success { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string? Error { get; set; }
        }
    }
}

