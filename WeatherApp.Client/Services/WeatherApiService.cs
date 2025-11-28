using System.Net.Http.Json;
using WeatherApp.Client.Models;

namespace WeatherApp.Client.Services
{
    public class WeatherApiService
    {
        private readonly HttpClient _httpClient;

        public WeatherApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WeatherData?> GetWeatherAsync(string city, string? country = null)
        {
            try
            {
                var url = country != null 
                    ? $"api/weather/{Uri.EscapeDataString(city)}?country={Uri.EscapeDataString(country)}"
                    : $"api/weather/{Uri.EscapeDataString(city)}";
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<WeatherData>();
                }
                else
                {
                    // Try to read error message from response
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API Error ({response.StatusCode}): {errorContent}");
                    
                    // If it's a 404, the API returns a message
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new Exception(errorContent);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        throw new Exception(errorContent);
                    }
                    else
                    {
                        throw new Exception($"Failed to fetch weather data. Status: {response.StatusCode}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Network/connection errors
                Console.WriteLine($"Network error: {ex.Message}");
                throw new Exception("Unable to connect to the weather service. Please ensure the API is running.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching weather: {ex.Message}");
                throw; // Re-throw to show the actual error message
            }
        }

        public async Task<WeatherData?> SearchWeatherAsync(WeatherRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/weather/search", request);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<WeatherData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching weather: {ex.Message}");
            }
            
            return null;
        }
    }
}

