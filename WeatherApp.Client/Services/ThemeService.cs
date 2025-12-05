using Microsoft.JSInterop;

namespace WeatherApp.Client.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private string _currentTheme = "light";

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeThemeAsync()
        {
            try
            {
                var theme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "theme");
                if (!string.IsNullOrEmpty(theme))
                {
                    _currentTheme = theme;
                }
                await ApplyThemeAsync(_currentTheme);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing theme: {ex.Message}");
            }
        }

        public async Task ToggleThemeAsync()
        {
            _currentTheme = _currentTheme == "light" ? "dark" : "light";
            await ApplyThemeAsync(_currentTheme);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme", _currentTheme);
        }

        public async Task SetThemeAsync(string theme)
        {
            _currentTheme = theme;
            await ApplyThemeAsync(_currentTheme);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme", _currentTheme);
        }

        public string GetCurrentTheme() => _currentTheme;

        public async Task ApplyWeatherThemeAsync(string mainCondition)
        {
            try
            {
                var weatherTheme = GetWeatherTheme(mainCondition);
                await _jsRuntime.InvokeVoidAsync("applyWeatherTheme", weatherTheme);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying weather theme: {ex.Message}");
            }
        }

        private async Task ApplyThemeAsync(string theme)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("applyTheme", theme);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private string GetWeatherTheme(string mainCondition)
        {
            return mainCondition.ToLower() switch
            {
                "clear" => "sunny",
                "clouds" => "cloudy",
                "rain" => "rainy",
                "drizzle" => "rainy",
                "thunderstorm" => "stormy",
                "snow" => "snowy",
                "mist" => "foggy",
                "fog" => "foggy",
                "haze" => "foggy",
                _ => "default"
            };
        }
    }
}

