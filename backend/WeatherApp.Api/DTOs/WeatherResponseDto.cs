namespace WeatherApp.Api.DTOs;

public class WeatherResponseDto
{
    public int Id { get; set; }
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public int Humidity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string IconCode { get; set; } = string.Empty;
    public string IconUrl => $"https://openweathermap.org/img/wn/{IconCode}@2x.png";
    public DateTime SearchedAt { get; set; }
}

public class ErrorResponseDto
{
    public string Error { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}
