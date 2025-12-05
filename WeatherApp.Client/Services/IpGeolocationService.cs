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
                // Using ipapi.co free service (no API key required for basic usage)
                var response = await _httpClient.GetAsync("https://ipapi.co/json/");
                
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<IpLocationResponse>();
                    if (data != null && data.Latitude.HasValue && data.Longitude.HasValue)
                    {
                        Console.WriteLine($"[IpGeolocationService] ✅ IP geolocation SUCCESS!");
                        Console.WriteLine($"[IpGeolocationService] Latitude: {data.Latitude.Value}");
                        Console.WriteLine($"[IpGeolocationService] Longitude: {data.Longitude.Value}");
                        Console.WriteLine($"[IpGeolocationService] Coordinates: ({data.Latitude.Value}, {data.Longitude.Value})");
                        if (!string.IsNullOrEmpty(data.City))
                        {
                            Console.WriteLine($"[IpGeolocationService] City: {data.City}");
                        }
                        if (!string.IsNullOrEmpty(data.Country))
                        {
                            Console.WriteLine($"[IpGeolocationService] Country: {data.Country}");
                        }
                        return (data.Latitude.Value, data.Longitude.Value);
                    }
                    else
                    {
                        Console.WriteLine("[IpGeolocationService] ❌ IP geolocation failed - invalid response data");
                    }
                }
                else
                {
                    Console.WriteLine($"[IpGeolocationService] ❌ IP geolocation failed - HTTP {response.StatusCode}");
                }
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

