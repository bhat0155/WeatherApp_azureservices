using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WeatherApp.Api.Data;
using WeatherApp.Api.Entities;
using WeatherApp.Api.Repositories;
using Xunit;

namespace WeatherApp.Tests.Repositories;

public class WeatherRepositoryTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SaveAsync_PersistsRecord_AndReturnsWithId()
    {
        using var db = CreateInMemoryDb();
        var repo = new WeatherRepository(db);

        var record = new WeatherRecord
        {
            City = "London",
            Country = "GB",
            Temperature = 15.5,
            FeelsLike = 13.0,
            Humidity = 80,
            Description = "clear sky",
            IconCode = "01d"
        };

        var saved = await repo.SaveAsync(record);

        saved.Id.Should().BeGreaterThan(0);
        db.WeatherRecords.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveAsync_SetsSearchedAt_ToUtcNow()
    {
        using var db = CreateInMemoryDb();
        var repo = new WeatherRepository(db);
        var before = DateTime.UtcNow.AddSeconds(-1);

        var saved = await repo.SaveAsync(new WeatherRecord { City = "Berlin", Country = "DE" });

        saved.SearchedAt.Should().BeAfter(before);
        saved.SearchedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsLastTenRecords_OrderedBySearchedAtDescending()
    {
        using var db = CreateInMemoryDb();
        var repo = new WeatherRepository(db);

        for (int i = 1; i <= 12; i++)
        {
            await db.WeatherRecords.AddAsync(new WeatherRecord
            {
                City = $"City{i}",
                Country = "US",
                SearchedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await db.SaveChangesAsync();

        var history = (await repo.GetHistoryAsync()).ToList();

        history.Should().HaveCount(10);
        history[0].City.Should().Be("City1");
        history[9].City.Should().Be("City10");
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsEmpty_WhenNoRecords()
    {
        using var db = CreateInMemoryDb();
        var repo = new WeatherRepository(db);

        var history = await repo.GetHistoryAsync();

        history.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingRecord_WhenCityAlreadyExists()
    {
        using var db = CreateInMemoryDb();
        var repo = new WeatherRepository(db);

        await repo.SaveAsync(new WeatherRecord { City = "Delhi", Country = "IN", Temperature = 38.0, Humidity = 60, Description = "haze", IconCode = "50d" });
        await repo.SaveAsync(new WeatherRecord { City = "Delhi", Country = "IN", Temperature = 40.0, Humidity = 65, Description = "sunny", IconCode = "01d" });

        db.WeatherRecords.Should().HaveCount(1);
        db.WeatherRecords.Single().Temperature.Should().Be(40.0);
        db.WeatherRecords.Single().Description.Should().Be("sunny");
    }

    [Fact]
    public async Task SaveAsync_IsCaseInsensitive_WhenMatchingCity()
    {
        using var db = CreateInMemoryDb();
        var repo = new WeatherRepository(db);

        await repo.SaveAsync(new WeatherRecord { City = "delhi", Country = "IN", Temperature = 38.0 });
        await repo.SaveAsync(new WeatherRecord { City = "Delhi", Country = "IN", Temperature = 40.0 });

        db.WeatherRecords.Should().HaveCount(1);
    }

    [Fact]
    public async Task ClearHistoryAsync_RemovesAllRecords()
    {
        using var db = CreateInMemoryDb();
        var repo = new WeatherRepository(db);

        await db.WeatherRecords.AddRangeAsync(
            new WeatherRecord { City = "A", Country = "AU" },
            new WeatherRecord { City = "B", Country = "BR" }
        );
        await db.SaveChangesAsync();

        await repo.ClearHistoryAsync();

        db.WeatherRecords.Should().BeEmpty();
    }
}
