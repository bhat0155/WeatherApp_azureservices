using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Api.Entities;

public class WeatherRecord
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Country { get; set; } = string.Empty;

    public double Temperature { get; set; }

    public double FeelsLike { get; set; }

    public int Humidity { get; set; }

    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(10)]
    public string IconCode { get; set; } = string.Empty;

    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
