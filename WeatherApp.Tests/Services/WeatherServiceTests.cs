using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using WeatherApp.API.Models;
using WeatherApp.API.Services;
using Xunit;

namespace WeatherApp.Tests.Services;

public class WeatherServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpHandlerMock;
    private readonly Mock<MongoDbService> _mongoDbServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly WeatherService _weatherService;

    public WeatherServiceTests()
    {
        _httpHandlerMock = new Mock<HttpMessageHandler>();
        _mongoDbServiceMock = new Mock<MongoDbService>(Mock.Of<IConfiguration>());
        _configurationMock = new Mock<IConfiguration>();

        var httpClient = new HttpClient(_httpHandlerMock.Object);
        
        _configurationMock.Setup(c => c["WeatherApi:Key"]).Returns("test-api-key");

        _weatherService = new WeatherService(
            httpClient,
            _mongoDbServiceMock.Object,
            _configurationMock.Object
        );
    }

    [Fact]
    public async Task GetWeatherAsync_ValidCity_ReturnsWeatherData()
    {
        // Arrange
        var city = "London";
        var mockResponse = CreateMockWeatherResponse(city, "GB", 15.5, 60.0);
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _weatherService.GetWeatherAsync(city);

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be(city);
        result.Temperature.Should().Be(15.5);
        result.Humidity.Should().Be(60.0);
        _mongoDbServiceMock.Verify(m => m.SaveWeatherDataAsync(It.IsAny<WeatherData>()), Times.Once);
    }

    [Fact]
    public async Task GetWeatherAsync_InvalidCity_ReturnsNull()
    {
        // Arrange
        var city = "InvalidCity123";
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"cod\":\"404\",\"message\":\"city not found\"}", Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _weatherService.GetWeatherAsync(city);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWeatherAsync_WithCountryCode_IncludesCountryInRequest()
    {
        // Arrange
        var city = "London";
        var country = "GB";
        var mockResponse = CreateMockWeatherResponse(city, country, 15.5, 60.0);
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.RequestUri!.ToString().Contains($"{city},{country}")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _weatherService.GetWeatherAsync(city, country);

        // Assert
        result.Should().NotBeNull();
        result!.Country.Should().Be(country);
    }

    [Fact]
    public async Task GetForecastAsync_ValidCity_ReturnsForecastData()
    {
        // Arrange
        var city = "London";
        var mockResponse = CreateMockForecastResponse(city, "GB");
        
        _httpHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _weatherService.GetForecastAsync(city);

        // Assert
        result.Should().NotBeNull();
        result!.City.Should().Be(city);
        result.Items.Should().NotBeEmpty();
    }

    private string CreateMockWeatherResponse(string city, string country, double temp, double humidity)
    {
        return JsonSerializer.Serialize(new
        {
            name = city,
            sys = new { country = country },
            main = new
            {
                temp = temp,
                feels_like = temp - 1,
                humidity = humidity,
                pressure = 1013
            },
            weather = new[]
            {
                new
                {
                    main = "Clear",
                    description = "clear sky",
                    icon = "01d"
                }
            },
            wind = new
            {
                speed = 5.5,
                deg = 180
            },
            clouds = new { all = 0 },
            coord = new
            {
                lat = 51.5074,
                lon = -0.1278
            },
            visibility = 10000
        });
    }

    private string CreateMockForecastResponse(string city, string country)
    {
        var forecastItems = new List<object>();
        var baseTime = DateTimeOffset.UtcNow;
        
        for (int i = 0; i < 40; i++)
        {
            forecastItems.Add(new
            {
                dt = ((DateTimeOffset)baseTime.AddHours(i * 3)).ToUnixTimeSeconds(),
                main = new
                {
                    temp = 15.0 + i,
                    feels_like = 14.0 + i,
                    temp_min = 12.0 + i,
                    temp_max = 18.0 + i,
                    humidity = 60,
                    pressure = 1013
                },
                weather = new[]
                {
                    new
                    {
                        main = "Clear",
                        description = "clear sky",
                        icon = "01d"
                    }
                },
                wind = new
                {
                    speed = 5.5,
                    deg = 180
                },
                clouds = new { all = 0 },
                rain = i % 3 == 0 ? new { @"3h" = 0.5 } : null
            });
        }

        return JsonSerializer.Serialize(new
        {
            city = new { name = city, country = country },
            list = forecastItems
        });
    }
}

