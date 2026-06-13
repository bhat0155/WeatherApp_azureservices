using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WeatherApp.Api.Configuration;
using WeatherApp.Api.Entities;
using WeatherApp.Api.Repositories;
using WeatherApp.Api.Services;
using Xunit;

namespace WeatherApp.Tests.Services;

public class WeatherServiceTests
{
    private readonly Mock<IWeatherRepository> _repoMock = new();
    private readonly Mock<ILogger<WeatherService>> _loggerMock = new();
    private readonly OpenWeatherMapOptions _options = new()
    {
        ApiKey = "test-key",
        BaseUrl = "https://api.openweathermap.org/data/2.5"
    };

    private WeatherService CreateService(HttpClient httpClient)
    {
        var optionsMock = Options.Create(_options);
        return new WeatherService(httpClient, _repoMock.Object, optionsMock, _loggerMock.Object);
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string json)
    {
        var handler = new MockHttpMessageHandler(statusCode, json);
        return new HttpClient(handler);
    }

    private static string BuildWeatherJson(string city = "London", string country = "GB") => JsonSerializer.Serialize(new
    {
        name = city,
        sys = new { country },
        main = new { temp = 15.5, feels_like = 13.0, humidity = 80 },
        weather = new[] { new { description = "clear sky", icon = "01d" } }
    });

    [Fact]
    public async Task GetWeatherAsync_ReturnsCorrectDto_WhenApiSucceeds()
    {
        var json = BuildWeatherJson("London", "GB");
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);

        _repoMock.Setup(r => r.SaveAsync(It.IsAny<WeatherRecord>()))
            .ReturnsAsync((WeatherRecord r) => { r.Id = 1; return r; });

        var service = CreateService(httpClient);
        var result = await service.GetWeatherAsync("London");

        result.City.Should().Be("London");
        result.Country.Should().Be("GB");
        result.Temperature.Should().Be(15.5);
        result.FeelsLike.Should().Be(13.0);
        result.Humidity.Should().Be(80);
        result.Description.Should().Be("clear sky");
        result.IconCode.Should().Be("01d");
    }

    [Fact]
    public async Task GetWeatherAsync_ThrowsCityNotFoundException_WhenApiReturns404()
    {
        var httpClient = CreateHttpClient(HttpStatusCode.NotFound, "{}");
        var service = CreateService(httpClient);

        var act = async () => await service.GetWeatherAsync("Nonexistentcity123");

        await act.Should().ThrowAsync<CityNotFoundException>()
            .WithMessage("*Nonexistentcity123*");
    }

    [Fact]
    public async Task GetWeatherAsync_ThrowsExternalServiceException_WhenApiIsDown()
    {
        var handler = new ThrowingHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var service = CreateService(httpClient);

        var act = async () => await service.GetWeatherAsync("London");

        await act.Should().ThrowAsync<ExternalServiceException>();
    }

    [Fact]
    public async Task GetWeatherAsync_ThrowsExternalServiceException_WhenApiReturns500()
    {
        var httpClient = CreateHttpClient(HttpStatusCode.InternalServerError, "{}");
        var service = CreateService(httpClient);

        var act = async () => await service.GetWeatherAsync("London");

        await act.Should().ThrowAsync<ExternalServiceException>();
    }

    [Fact]
    public async Task GetWeatherAsync_SavesRecordToRepository()
    {
        var json = BuildWeatherJson("Paris", "FR");
        var httpClient = CreateHttpClient(HttpStatusCode.OK, json);

        _repoMock.Setup(r => r.SaveAsync(It.IsAny<WeatherRecord>()))
            .ReturnsAsync((WeatherRecord r) => { r.Id = 42; return r; });

        var service = CreateService(httpClient);
        await service.GetWeatherAsync("Paris");

        _repoMock.Verify(r => r.SaveAsync(It.Is<WeatherRecord>(rec =>
            rec.City == "Paris" && rec.Country == "FR")), Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsMappedDtos()
    {
        var records = new List<WeatherRecord>
        {
            new() { Id = 1, City = "London", Country = "GB", Temperature = 15, FeelsLike = 13, Humidity = 80, Description = "clear", IconCode = "01d", SearchedAt = DateTime.UtcNow },
            new() { Id = 2, City = "Paris", Country = "FR", Temperature = 20, FeelsLike = 18, Humidity = 70, Description = "sunny", IconCode = "01d", SearchedAt = DateTime.UtcNow }
        };

        _repoMock.Setup(r => r.GetHistoryAsync(10)).ReturnsAsync(records);

        var httpClient = CreateHttpClient(HttpStatusCode.OK, "{}");
        var service = CreateService(httpClient);
        var result = (await service.GetHistoryAsync()).ToList();

        result.Should().HaveCount(2);
        result[0].City.Should().Be("London");
        result[1].City.Should().Be("Paris");
    }

    [Fact]
    public async Task ClearHistoryAsync_CallsRepository()
    {
        _repoMock.Setup(r => r.ClearHistoryAsync()).Returns(Task.CompletedTask);
        var httpClient = CreateHttpClient(HttpStatusCode.OK, "{}");
        var service = CreateService(httpClient);

        await service.ClearHistoryAsync();

        _repoMock.Verify(r => r.ClearHistoryAsync(), Times.Once);
    }
}

// Test helpers
internal class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;

    public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, Encoding.UTF8, "application/json")
        });
    }
}

internal class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        throw new HttpRequestException("Connection refused");
    }
}
