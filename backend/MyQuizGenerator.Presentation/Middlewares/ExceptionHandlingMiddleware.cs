using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Presentation.Common.Responses;

namespace MyQuizGenerator.Presentation.Middlewares;

/// <summary>
/// Global exception handling middleware that catches all unhandled exceptions
/// and returns a standardized API response
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        ApiResponse errorResponse;

        switch (exception)
        {
            case AppException appException:
                // Handle custom application exceptions
                response.StatusCode = appException.StatusCode;
                errorResponse = ApiResponse.Fail(
                    appException.Message,
                    appException.StatusCode,
                    appException.Errors
                );

                // Log as warning for client errors (4xx)
                if (appException.StatusCode < 500)
                {
                    _logger.LogWarning(
                        "Client error: {StatusCode} - {Message}",
                        appException.StatusCode,
                        appException.Message
                    );
                }
                else
                {
                    _logger.LogError(
                        exception,
                        "Application error: {StatusCode} - {Message}",
                        appException.StatusCode,
                        appException.Message
                    );
                }
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse = ApiResponse.Fail("Unauthorized", 401);
                _logger.LogWarning("Unauthorized access attempt");
                break;

            case KeyNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse = ApiResponse.Fail("Resource not found", 404);
                _logger.LogWarning("Resource not found: {Message}", exception.Message);
                break;

            case ArgumentException:
            case InvalidOperationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse = ApiResponse.Fail(exception.Message, 400);
                _logger.LogWarning("Bad request: {Message}", exception.Message);
                break;

            default:
                // Handle unexpected exceptions
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse = ApiResponse.Fail("An unexpected error occurred", 500);

                // Log full exception details for unexpected errors
                _logger.LogError(
                    exception,
                    "Unhandled exception: {Message}",
                    exception.Message
                );
                break;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await response.WriteAsync(result);
    }
}

/// <summary>
/// Extension method to register the exception handling middleware
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
