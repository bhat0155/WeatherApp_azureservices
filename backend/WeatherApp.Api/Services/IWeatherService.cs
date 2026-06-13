using WeatherApp.Api.DTOs;

namespace WeatherApp.Api.Services;

public interface IWeatherService
{
    /// <summary>Fetches weather for a city, persists it, and returns the DTO.</summary>
    Task<WeatherResponseDto> GetWeatherAsync(string city);

    /// <summary>Returns the last 10 weather searches.</summary>
    Task<IEnumerable<WeatherResponseDto>> GetHistoryAsync();

    /// <summary>Clears all weather history.</summary>
    Task ClearHistoryAsync();
}
