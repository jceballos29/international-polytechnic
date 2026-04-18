using System.Net;
using System.Text.Json;
using Identity.Domain.Exceptions;

namespace Identity.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment env
    )
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(
                "DomainException: {ErrorCode} - {Message}",
                ex.ErrorCode,
                ex.Message
            );

            await WriteErrorResponse(context, ex.HttpStatusCode, ex.ErrorCode, ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("ArgumentException: {Message}", ex.Message);

            await WriteErrorResponse(context, 400, "INVALID_REQUEST", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            var message = _env.IsDevelopment()
                ? ex.Message
                : "Ocurrió un error interno. Intenta de nuevo.";

            await WriteErrorResponse(context, 500, "INTERNAL_ERROR", message);
        }
    }

    private static async Task WriteErrorResponse(
        HttpContext context,
        int statusCode,
        string errorCode,
        string message
    )
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = errorCode,
            error_description = message,
            status = statusCode,
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(
                response,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }
            )
        );
    }
}
