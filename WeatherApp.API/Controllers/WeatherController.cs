using Microsoft.AspNetCore.Mvc;
using WeatherApp.API.Models;
using WeatherApp.API.Services;

namespace WeatherApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherController : ControllerBase
    {
        private readonly WeatherService _weatherService;

        public WeatherController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet("{city}")]
        public async Task<ActionResult<WeatherData>> GetWeather(string city, [FromQuery] string? country = null)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return BadRequest("City name is required");
            }

            var weather = await _weatherService.GetWeatherAsync(city, country);
            if (weather == null)
            {
                return NotFound($"Weather data not found for {city}");
            }

            return Ok(weather);
        }

        [HttpPost("search")]
        public async Task<ActionResult<WeatherData>> SearchWeather([FromBody] WeatherRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.City))
            {
                return BadRequest("City name is required");
            }

            var weather = await _weatherService.GetWeatherAsync(request.City, request.Country);
            if (weather == null)
            {
                return NotFound($"Weather data not found for {request.City}");
            }

            return Ok(weather);
        }

        [HttpGet("forecast/{city}")]
        public async Task<ActionResult<ForecastData>> GetForecast(string city, [FromQuery] string? country = null)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return BadRequest("City name is required");
            }

            var forecast = await _weatherService.GetForecastAsync(city, country);
            if (forecast == null)
            {
                return NotFound($"Forecast data not found for {city}");
            }

            return Ok(forecast);
        }

        [HttpPost("location")]
        public async Task<ActionResult<WeatherData>> GetWeatherByLocation([FromBody] LocationRequest request)
        {
            var weather = await _weatherService.GetWeatherByCoordinatesAsync(request.Latitude, request.Longitude);
            if (weather == null)
            {
                return NotFound("Weather data not found for the specified location");
            }

            return Ok(weather);
        }

        [HttpPost("forecast/location")]
        public async Task<ActionResult<ForecastData>> GetForecastByLocation([FromBody] LocationRequest request)
        {
            var forecast = await _weatherService.GetForecastByCoordinatesAsync(request.Latitude, request.Longitude);
            if (forecast == null)
            {
                return NotFound("Forecast data not found for the specified location");
            }

            return Ok(forecast);
        }
    }
}

