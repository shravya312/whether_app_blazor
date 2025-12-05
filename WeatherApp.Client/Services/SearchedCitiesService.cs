using WeatherApp.Client.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace WeatherApp.Client.Services
{
    public class SearchedCity
    {
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public DateTime LastSearched { get; set; } = DateTime.UtcNow;
        public int SearchCount { get; set; } = 1;
    }

    public class SearchedCitiesService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "searched_cities";

        public SearchedCitiesService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<List<SearchedCity>> GetAllSearchedCitiesAsync(string userId)
        {
            try
            {
                var key = $"{StorageKey}_{userId}";
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                
                if (string.IsNullOrEmpty(json))
                    return new List<SearchedCity>();

                var cities = JsonSerializer.Deserialize<List<SearchedCity>>(json);
                return cities ?? new List<SearchedCity>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching searched cities: {ex.Message}");
                return new List<SearchedCity>();
            }
        }

        public async Task AddSearchedCityAsync(string userId, string city, string country)
        {
            try
            {
                var cities = await GetAllSearchedCitiesAsync(userId);
                
                var existing = cities.FirstOrDefault(c => 
                    c.City.Equals(city, StringComparison.OrdinalIgnoreCase) && 
                    c.Country.Equals(country, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.SearchCount++;
                    existing.LastSearched = DateTime.UtcNow;
                }
                else
                {
                    cities.Add(new SearchedCity
                    {
                        City = city,
                        Country = country,
                        LastSearched = DateTime.UtcNow,
                        SearchCount = 1
                    });
                }

                // Keep only last 100 cities
                if (cities.Count > 100)
                {
                    cities = cities.OrderByDescending(c => c.LastSearched).Take(100).ToList();
                }

                var key = $"{StorageKey}_{userId}";
                var json = JsonSerializer.Serialize(cities);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding searched city: {ex.Message}");
            }
        }

        public async Task ClearSearchedCitiesAsync(string userId)
        {
            try
            {
                var key = $"{StorageKey}_{userId}";
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing searched cities: {ex.Message}");
            }
        }
    }
}

