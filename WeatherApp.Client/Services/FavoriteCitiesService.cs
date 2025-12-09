using WeatherApp.Client.Models;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WeatherApp.Client.Services
{
    public class FavoriteCitiesService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "favorite_cities";

        public FavoriteCitiesService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<List<FavoriteCity>> GetFavoriteCitiesAsync(string userId)
        {
            try
            {
                var key = $"{StorageKey}_{userId}";
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                
                if (string.IsNullOrEmpty(json))
                    return new List<FavoriteCity>();

                var cities = JsonSerializer.Deserialize<List<FavoriteCity>>(json);
                if (cities == null)
                    return new List<FavoriteCity>();

                // Order by search count (descending), then by last searched (descending)
                return cities
                    .OrderByDescending(c => c.SearchCount)
                    .ThenByDescending(c => c.LastSearched)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching favorite cities: {ex.Message}");
                return new List<FavoriteCity>();
            }
        }

        public async Task<bool> AddFavoriteCityAsync(string userId, string city, string country)
        {
            try
            {
                var favorites = await GetFavoriteCitiesAsync(userId);
                
                if (favorites.Any(f => f.City.Equals(city, StringComparison.OrdinalIgnoreCase) && 
                                     f.Country.Equals(country, StringComparison.OrdinalIgnoreCase)))
                {
                    return false; // Already exists
                }

                favorites.Add(new FavoriteCity
                {
                    Id = Guid.NewGuid().ToString(),
                    City = city,
                    Country = country,
                    AddedAt = DateTime.UtcNow,
                    SearchCount = 0,
                    LastSearched = DateTime.UtcNow
                });

                var key = $"{StorageKey}_{userId}";
                var json = JsonSerializer.Serialize(favorites);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
                
                // Automatically add new city to monitored cities (checked by default)
                await AddCityToMonitoredCitiesAsync(userId, city, country);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding favorite city: {ex.Message}");
                return false;
            }
        }
        
        private async Task AddCityToMonitoredCitiesAsync(string userId, string city, string country)
        {
            try
            {
                var alertSettingsKey = $"alert_settings_{userId}";
                var settingsJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", alertSettingsKey);
                
                if (!string.IsNullOrEmpty(settingsJson))
                {
                    var settings = JsonSerializer.Deserialize<AlertSettings>(settingsJson);
                    if (settings != null)
                    {
                        if (settings.MonitoredCities == null)
                        {
                            settings.MonitoredCities = new List<string>();
                        }
                        
                        var cityKey = $"{city}, {country}";
                        if (!settings.MonitoredCities.Contains(cityKey))
                        {
                            settings.MonitoredCities.Add(cityKey);
                            var updatedJson = JsonSerializer.Serialize(settings);
                            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", alertSettingsKey, updatedJson);
                            Console.WriteLine($"[FavoriteCitiesService] ✅ Auto-added {cityKey} to monitored cities");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FavoriteCitiesService] Error adding city to monitored cities: {ex.Message}");
            }
        }

        public async Task<bool> RemoveFavoriteCityAsync(string userId, string favoriteId)
        {
            try
            {
                var favorites = await GetFavoriteCitiesAsync(userId);
                favorites.RemoveAll(f => f.Id == favoriteId);

                var key = $"{StorageKey}_{userId}";
                var json = JsonSerializer.Serialize(favorites);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing favorite city: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> IsFavoriteAsync(string userId, string city, string country)
        {
            var favorites = await GetFavoriteCitiesAsync(userId);
            return favorites.Any(f => f.City.Equals(city, StringComparison.OrdinalIgnoreCase) && 
                                     f.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
        }

        public async Task IncrementSearchCountAsync(string userId, string city, string country)
        {
            try
            {
                var key = $"{StorageKey}_{userId}";
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                
                if (string.IsNullOrEmpty(json))
                    return;

                var favorites = JsonSerializer.Deserialize<List<FavoriteCity>>(json);
                if (favorites == null)
                    return;

                var favorite = favorites.FirstOrDefault(f => 
                    f.City.Equals(city, StringComparison.OrdinalIgnoreCase) && 
                    f.Country.Equals(country, StringComparison.OrdinalIgnoreCase));

                if (favorite != null)
                {
                    favorite.SearchCount++;
                    favorite.LastSearched = DateTime.UtcNow;
                    
                    var updatedJson = JsonSerializer.Serialize(favorites);
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, updatedJson);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error incrementing search count: {ex.Message}");
            }
        }

        public async Task<bool> AddOrUpdateFavoriteAsync(string userId, string city, string country)
        {
            try
            {
                var key = $"{StorageKey}_{userId}";
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                
                var favorites = string.IsNullOrEmpty(json) 
                    ? new List<FavoriteCity>() 
                    : JsonSerializer.Deserialize<List<FavoriteCity>>(json) ?? new List<FavoriteCity>();

                var existing = favorites.FirstOrDefault(f => 
                    f.City.Equals(city, StringComparison.OrdinalIgnoreCase) && 
                    f.Country.Equals(country, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // Already exists, just increment search count
                    existing.SearchCount++;
                    existing.LastSearched = DateTime.UtcNow;
                }
                else
                {
                    // Add new favorite with initial search count
                    favorites.Add(new FavoriteCity
                    {
                        Id = Guid.NewGuid().ToString(),
                        City = city,
                        Country = country,
                        AddedAt = DateTime.UtcNow,
                        SearchCount = 1,
                        LastSearched = DateTime.UtcNow
                    });
                    
                    // Automatically add new city to monitored cities (checked by default)
                    await AddCityToMonitoredCitiesAsync(userId, city, country);
                }

                var updatedJson = JsonSerializer.Serialize(favorites);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, updatedJson);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding/updating favorite city: {ex.Message}");
                return false;
            }
        }
    }
}

