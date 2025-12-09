using System.Net.Http.Json;
using WeatherApp.Client.Models;

namespace WeatherApp.Client.Services
{
    public class IpGeolocationService
    {
        private readonly HttpClient _httpClient;

        public IpGeolocationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(double Latitude, double Longitude)?> GetLocationByIpAsync()
        {
            try
            {
                Console.WriteLine("[IpGeolocationService] Attempting to get location by IP address...");
                
                // Try multiple IP geolocation services for better accuracy
                // Prioritize services that are more accurate for India/Asia region
                var services = new[]
                {
                    "https://ipapi.co/json/",           // Good accuracy for India
                    "https://ip-api.com/json/",         // Free tier, decent accuracy
                    "https://ipgeolocation.io/json/",   // Good for Asia region
                    "https://geojs.io/geo.json",        // Lightweight, good accuracy
                    "https://freeipapi.com/api/json/"   // Fallback
                };
                
                foreach (var serviceUrl in services)
                {
                    try
                    {
                        Console.WriteLine($"[IpGeolocationService] Trying service: {serviceUrl}");
                        var response = await _httpClient.GetAsync(serviceUrl);
                        
                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var data = await response.Content.ReadFromJsonAsync<IpLocationResponse>();
                            
                            if (data != null && data.Latitude.HasValue && data.Longitude.HasValue)
                            {
                                var lat = data.Latitude.Value;
                                var lon = data.Longitude.Value;
                                
                                Console.WriteLine($"[IpGeolocationService] ✅ IP geolocation SUCCESS!");
                                Console.WriteLine($"[IpGeolocationService] Latitude: {lat}");
                                Console.WriteLine($"[IpGeolocationService] Longitude: {lon}");
                                Console.WriteLine($"[IpGeolocationService] Coordinates: ({lat}, {lon})");
                                
                                if (!string.IsNullOrEmpty(data.City))
                                {
                                    Console.WriteLine($"[IpGeolocationService] City: {data.City}");
                                }
                                
                                // Check if coordinates are suspiciously far from expected location
                                // Bangalore area: ~12.97°N, 77.59°E
                                // If IP geolocation gives coordinates far from user's expected location, log warning
                                var bangaloreLat = 12.9716;
                                var bangaloreLon = 77.5946;
                                var distanceFromBangalore = Math.Sqrt(Math.Pow(lat - bangaloreLat, 2) + Math.Pow(lon - bangaloreLon, 2));
                                
                                if (distanceFromBangalore > 0.5) // More than ~55km from Bangalore
                                {
                                    Console.WriteLine($"[IpGeolocationService] ⚠️ WARNING: IP geolocation may be inaccurate!");
                                    Console.WriteLine($"[IpGeolocationService] ⚠️ Coordinates ({lat}, {lon}) are {distanceFromBangalore:F2} degrees (~{distanceFromBangalore * 111:F0} km) from Bangalore center");
                                    Console.WriteLine($"[IpGeolocationService] ⚠️ Consider using browser geolocation for more accurate results");
                                }
                                
                                return (lat, lon);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[IpGeolocationService] Service {serviceUrl} failed: {ex.Message}");
                        continue; // Try next service
                    }
                }
                
                Console.WriteLine("[IpGeolocationService] ❌ All IP geolocation services failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IpGeolocationService] ❌ IP geolocation error: {ex.Message}");
            }
            
            return null;
        }

        private class IpLocationResponse
        {
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public string? City { get; set; }
            public string? Country { get; set; }
        }
    }
}

