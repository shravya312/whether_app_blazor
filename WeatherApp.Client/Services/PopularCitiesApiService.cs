using System.Net.Http.Json;

namespace WeatherApp.Client.Services
{
    public class PopularCitiesApiService
    {
        private readonly HttpClient _httpClient;

        public PopularCitiesApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> GetPopularCitiesAsync(int limit = 5)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/popularcities?limit={limit}");
                
                if (response.IsSuccessStatusCode)
                {
                    var cities = await response.Content.ReadFromJsonAsync<List<string>>();
                    return cities ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching popular cities: {ex.Message}");
            }
            
            return new List<string>();
        }
    }
}

