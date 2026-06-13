using System.Net;
using System.Text.Json;
using WeatherApp.Api.Services;

namespace WeatherApp.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (CityNotFoundException ex)
        {
            _logger.LogWarning(ex, "City not found");
            await WriteError(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogError(ex, "External service error");
            await WriteError(context, HttpStatusCode.ServiceUnavailable, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteError(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteError(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new
        {
            error = message,
            statusCode = (int)statusCode
        });

        await context.Response.WriteAsync(body);
    }
}
