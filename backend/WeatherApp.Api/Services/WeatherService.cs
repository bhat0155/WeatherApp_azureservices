using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WeatherApp.Api.Configuration;
using WeatherApp.Api.DTOs;
using WeatherApp.Api.Entities;
using WeatherApp.Api.Repositories;

namespace WeatherApp.Api.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IWeatherRepository _repository;
    private readonly OpenWeatherMapOptions _options;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        HttpClient httpClient,
        IWeatherRepository repository,
        IOptions<OpenWeatherMapOptions> options,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<WeatherResponseDto> GetWeatherAsync(string city)
    {
        var url = $"{_options.BaseUrl}/weather?q={Uri.EscapeDataString(city)}&appid={_options.ApiKey}&units=metric";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenWeatherMap API is unreachable");
            throw new ExternalServiceException("Weather service is currently unavailable.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("City not found: {City}", city);
            throw new CityNotFoundException($"City '{city}' was not found.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenWeatherMap returned {StatusCode} for city {City}", response.StatusCode, city);
            throw new ExternalServiceException("Weather service returned an unexpected error.");
        }

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var record = new WeatherRecord
        {
            City = root.GetProperty("name").GetString() ?? city,
            Country = root.GetProperty("sys").GetProperty("country").GetString() ?? string.Empty,
            Temperature = root.GetProperty("main").GetProperty("temp").GetDouble(),
            FeelsLike = root.GetProperty("main").GetProperty("feels_like").GetDouble(),
            Humidity = root.GetProperty("main").GetProperty("humidity").GetInt32(),
            Description = root.GetProperty("weather")[0].GetProperty("description").GetString() ?? string.Empty,
            IconCode = root.GetProperty("weather")[0].GetProperty("icon").GetString() ?? string.Empty,
        };

        var saved = await _repository.SaveAsync(record);
        return MapToDto(saved);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WeatherResponseDto>> GetHistoryAsync()
    {
        var records = await _repository.GetHistoryAsync();
        return records.Select(MapToDto);
    }

    /// <inheritdoc/>
    public async Task ClearHistoryAsync()
    {
        await _repository.ClearHistoryAsync();
    }

    private static WeatherResponseDto MapToDto(WeatherRecord r) => new()
    {
        Id = r.Id,
        City = r.City,
        Country = r.Country,
        Temperature = r.Temperature,
        FeelsLike = r.FeelsLike,
        Humidity = r.Humidity,
        Description = r.Description,
        IconCode = r.IconCode,
        SearchedAt = r.SearchedAt,
    };
}

public class CityNotFoundException : Exception
{
    public CityNotFoundException(string message) : base(message) { }
}

public class ExternalServiceException : Exception
{
    public ExternalServiceException(string message) : base(message) { }
}
