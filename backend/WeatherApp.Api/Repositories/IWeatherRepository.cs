using WeatherApp.Api.Entities;

namespace WeatherApp.Api.Repositories;

public interface IWeatherRepository
{
    /// <summary>Saves a weather record to the database.</summary>
    Task<WeatherRecord> SaveAsync(WeatherRecord record);

    /// <summary>Returns the last 10 weather searches ordered by most recent.</summary>
    Task<IEnumerable<WeatherRecord>> GetHistoryAsync(int count = 10);

    /// <summary>Deletes all weather history records.</summary>
    Task ClearHistoryAsync();
}
