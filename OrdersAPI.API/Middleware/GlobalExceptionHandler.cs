using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace OrdersAPI.API.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        var (statusCode, title, errors) = exception switch
        {
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                new Dictionary<string, string[]> { ["error"] = [exception.Message] }
            ),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                new Dictionary<string, string[]> { ["error"] = [exception.Message] }
            ),
            InvalidOperationException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                new Dictionary<string, string[]> { ["error"] = [exception.Message] }
            ),
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                new Dictionary<string, string[]> { ["error"] = ["An unexpected error occurred"] }
            )
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var problemDetails = new
        {
            type = "https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            errors,
            traceId = httpContext.TraceIdentifier
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
