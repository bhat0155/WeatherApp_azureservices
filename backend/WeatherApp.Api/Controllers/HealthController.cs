using Microsoft.AspNetCore.Mvc;

namespace WeatherApp.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    /// <summary>Returns 200 OK with server timestamp to confirm the API is alive.</summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
