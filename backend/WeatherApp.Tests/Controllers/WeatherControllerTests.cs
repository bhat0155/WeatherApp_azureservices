using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Api.Controllers;
using WeatherApp.Api.DTOs;
using WeatherApp.Api.Services;
using Xunit;

namespace WeatherApp.Tests.Controllers;

public class WeatherControllerTests
{
    private readonly Mock<IWeatherService> _serviceMock = new();
    private readonly Mock<ILogger<WeatherController>> _loggerMock = new();

    private WeatherController CreateController() =>
        new(_serviceMock.Object, _loggerMock.Object);

    private static WeatherResponseDto SampleDto() => new()
    {
        Id = 1,
        City = "London",
        Country = "GB",
        Temperature = 15.5,
        FeelsLike = 13.0,
        Humidity = 80,
        Description = "clear sky",
        IconCode = "01d",
        SearchedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetWeather_Returns200_WithValidCity()
    {
        _serviceMock.Setup(s => s.GetWeatherAsync("London")).ReturnsAsync(SampleDto());
        var controller = CreateController();

        var result = await controller.GetWeather("London");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<WeatherResponseDto>().Subject;
        dto.City.Should().Be("London");
    }

    [Fact]
    public async Task GetWeather_Returns404_WhenCityNotFound()
    {
        _serviceMock.Setup(s => s.GetWeatherAsync("Fakecity999"))
            .ThrowsAsync(new CityNotFoundException("City 'Fakecity999' was not found."));

        // Exception is handled by middleware; controller propagates it
        var controller = CreateController();
        var act = async () => await controller.GetWeather("Fakecity999");

        await act.Should().ThrowAsync<CityNotFoundException>();
    }

    [Fact]
    public async Task GetWeather_Returns400_ForEmptyCity()
    {
        var controller = CreateController();

        var result = await controller.GetWeather("   ");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetWeather_Returns400_ForNullCity()
    {
        var controller = CreateController();

        var result = await controller.GetWeather(string.Empty);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetHistory_Returns200_WithList()
    {
        _serviceMock.Setup(s => s.GetHistoryAsync())
            .ReturnsAsync(new[] { SampleDto() });

        var controller = CreateController();
        var result = await controller.GetHistory();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<WeatherResponseDto>>().Subject;
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHistory_Returns200_WithEmptyList()
    {
        _serviceMock.Setup(s => s.GetHistoryAsync())
            .ReturnsAsync(Enumerable.Empty<WeatherResponseDto>());

        var controller = CreateController();
        var result = await controller.GetHistory();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ClearHistory_Returns204()
    {
        _serviceMock.Setup(s => s.ClearHistoryAsync()).Returns(Task.CompletedTask);
        var controller = CreateController();

        var result = await controller.ClearHistory();

        result.Should().BeOfType<NoContentResult>();
    }
}

public class HealthControllerTests
{
    [Fact]
    public void Get_Returns200_WithTimestamp()
    {
        var controller = new HealthController();

        var result = controller.Get();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }
}
