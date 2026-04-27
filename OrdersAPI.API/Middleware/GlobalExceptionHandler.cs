using FluentValidation;
using OrdersAPI.Domain.Exceptions;

namespace OrdersAPI.API.Middleware;

public class GlobalExceptionHandler(
    RequestDelegate next,
    ILogger<GlobalExceptionHandler> logger)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception exception)
        {
            if (httpContext.Response.HasStarted)
            {
                logger.LogError(exception, "An error occurred after the response had already started: {Message}", exception.Message);
                throw;
            }

            await HandleExceptionAsync(httpContext, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                new Dictionary<string, string[]> { ["error"] = [exception.Message] }
            ),
            ConflictException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                new Dictionary<string, string[]> { ["error"] = [exception.Message] }
            ),
            BusinessException => (
                StatusCodes.Status422UnprocessableEntity,
                "Business Rule Violation",
                new Dictionary<string, string[]> { ["error"] = [exception.Message] }
            ),
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

        if (statusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "An unexpected error occurred: {Message}", exception.Message);
        else if (exception is BusinessException)
        {
            // Expected business-rule responses are returned to the client as 422.
        }
        else
            logger.LogWarning("{ExceptionType}: {Message}", exception.GetType().Name, exception.Message);

        httpContext.Response.Clear();
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var problemDetails = new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            errors,
            traceId = httpContext.TraceIdentifier
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, httpContext.RequestAborted);
    }
}
