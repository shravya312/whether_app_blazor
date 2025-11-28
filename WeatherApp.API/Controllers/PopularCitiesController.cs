using Microsoft.AspNetCore.Mvc;
using WeatherApp.API.Services;

namespace WeatherApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PopularCitiesController : ControllerBase
    {
        private readonly WeatherService _weatherService;

        public PopularCitiesController(WeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> GetPopularCities([FromQuery] int limit = 5)
        {
            var cities = await _weatherService.GetPopularCitiesAsync(limit);
            return Ok(cities);
        }
    }
}

