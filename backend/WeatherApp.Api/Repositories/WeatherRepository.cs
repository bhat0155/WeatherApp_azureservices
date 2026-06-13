using Microsoft.EntityFrameworkCore;
using WeatherApp.Api.Data;
using WeatherApp.Api.Entities;

namespace WeatherApp.Api.Repositories;

public class WeatherRepository : IWeatherRepository
{
    private readonly AppDbContext _db;

    public WeatherRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<WeatherRecord> SaveAsync(WeatherRecord record)
    {
        record.SearchedAt = DateTime.UtcNow;
        _db.WeatherRecords.Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WeatherRecord>> GetHistoryAsync(int count = 10)
    {
        return await _db.WeatherRecords
            .OrderByDescending(r => r.SearchedAt)
            .Take(count)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task ClearHistoryAsync()
    {
        _db.WeatherRecords.RemoveRange(_db.WeatherRecords);
        await _db.SaveChangesAsync();
    }
}
