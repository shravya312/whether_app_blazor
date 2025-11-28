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
    }
}

