using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using WeatherApp.API;
using WeatherApp.API.Models;
using Xunit;

namespace WeatherApp.Tests.Integration;

public class WeatherApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public WeatherApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Use test configuration
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["WeatherApi:Key"] = "test-key",
                    ["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
                    ["MongoDB:DatabaseName"] = "WeatherAppTest",
                    ["PushNotifications:VapidPublicKey"] = "test-public-key",
                    ["PushNotifications:VapidPrivateKey"] = "test-private-key",
                    ["PushNotifications:VapidSubject"] = "mailto:test@example.com"
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetWeather_InvalidCity_ReturnsNotFound()
    {
        // Arrange
        var city = "InvalidCity12345";

        // Act
        var response = await _client.GetAsync($"/api/weather/{city}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWeather_EmptyCity_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/weather/ ");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPopularCities_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/popularcities?limit=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cities = await response.Content.ReadFromJsonAsync<List<string>>();
        cities.Should().NotBeNull();
    }

    [Fact]
    public async Task GetWeatherHistory_ValidCity_ReturnsOk()
    {
        // Arrange
        var city = "London";

        // Act
        var response = await _client.GetAsync($"/api/weather/history/{city}?days=30");

        // Assert
        // May return empty list if no history, but should not error
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

