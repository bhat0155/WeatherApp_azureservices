using Microsoft.EntityFrameworkCore;
using WeatherApp.Api.Entities;

namespace WeatherApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<WeatherRecord> WeatherRecords => Set<WeatherRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WeatherRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(10);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.IconCode).HasMaxLength(10);
            entity.Property(e => e.SearchedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
