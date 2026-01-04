using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WeatherApp.API.Controllers;
using WeatherApp.API.Models;
using WeatherApp.API.Services;
using Xunit;

namespace WeatherApp.Tests.Controllers;

public class WeatherControllerTests
{
    private readonly Mock<WeatherService> _weatherServiceMock;
    private readonly Mock<MongoDbService> _mongoDbServiceMock;
    private readonly WeatherController _controller;

    public WeatherControllerTests()
    {
        _weatherServiceMock = new Mock<WeatherService>(
            Mock.Of<System.Net.Http.HttpClient>(),
            Mock.Of<MongoDbService>(),
            Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>()
        );
        _mongoDbServiceMock = new Mock<MongoDbService>(Mock.Of<Microsoft.Extensions.Configuration.IConfiguration>());
        
        _controller = new WeatherController(_weatherServiceMock.Object, _mongoDbServiceMock.Object);
    }

    [Fact]
    public async Task GetWeather_ValidCity_ReturnsOkResult()
    {
        // Arrange
        var city = "London";
        var expectedWeather = new WeatherData
        {
            City = city,
            Country = "GB",
            Temperature = 15.5,
            Humidity = 60.0
        };

        _weatherServiceMock
            .Setup(s => s.GetWeatherAsync(city, null))
            .ReturnsAsync(expectedWeather);

        // Act
        var result = await _controller.GetWeather(city);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var weather = okResult.Value.Should().BeOfType<WeatherData>().Subject;
        weather.City.Should().Be(city);
    }

    [Fact]
    public async Task GetWeather_InvalidCity_ReturnsNotFound()
    {
        // Arrange
        var city = "InvalidCity123";
        
        _weatherServiceMock
            .Setup(s => s.GetWeatherAsync(city, null))
            .ReturnsAsync((WeatherData?)null);

        // Act
        var result = await _controller.GetWeather(city);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetWeather_EmptyCity_ReturnsBadRequest()
    {
        // Arrange
        var city = "";

        // Act
        var result = await _controller.GetWeather(city);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetForecast_ValidCity_ReturnsOkResult()
    {
        // Arrange
        var city = "London";
        var expectedForecast = new ForecastData
        {
            City = city,
            Country = "GB",
            Items = new List<ForecastItem>
            {
                new ForecastItem
                {
                    DateTime = DateTime.UtcNow,
                    Temperature = 15.0,
                    Humidity = 60.0
                }
            }
        };

        _weatherServiceMock
            .Setup(s => s.GetForecastAsync(city, null))
            .ReturnsAsync(expectedForecast);

        // Act
        var result = await _controller.GetForecast(city);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var forecast = okResult.Value.Should().BeOfType<ForecastData>().Subject;
        forecast.City.Should().Be(city);
        forecast.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWeatherHistory_ValidCity_ReturnsOkResult()
    {
        // Arrange
        var city = "London";
        var country = "GB";
        var days = 30;
        var expectedHistory = new List<WeatherData>
        {
            new WeatherData { City = city, Country = country, Temperature = 15.0, Timestamp = DateTime.UtcNow }
        };

        _mongoDbServiceMock
            .Setup(s => s.GetWeatherHistoryByCityAndCountryAsync(city, country, days))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _controller.GetWeatherHistory(city, country, days);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var history = okResult.Value.Should().BeOfType<List<WeatherData>>().Subject;
        history.Should().NotBeEmpty();
        history.First().City.Should().Be(city);
    }
}

