using Microsoft.AspNetCore.Mvc;
using WeatherApp.Api.Services;

namespace WeatherApp.Api.Controllers;

[ApiController]
[Route("api/weather")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>Fetches current weather for a city and saves to history.</summary>
    [HttpGet("{city}")]
    public async Task<IActionResult> GetWeather(string city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return BadRequest(new { error = "City name cannot be empty.", statusCode = 400 });

        city = city.Trim();

        _logger.LogInformation("Fetching weather for city: {City}", city);
        var result = await _weatherService.GetWeatherAsync(city);
        return Ok(result);
    }

    /// <summary>Returns last 10 weather searches.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _weatherService.GetHistoryAsync();
        return Ok(history);
    }

    /// <summary>Clears all weather search history.</summary>
    [HttpDelete("history")]
    public async Task<IActionResult> ClearHistory()
    {
        await _weatherService.ClearHistoryAsync();
        return NoContent();
    }
}
