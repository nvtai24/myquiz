using System.Diagnostics;

namespace MyQuizGenerator.Presentation.Middlewares;

/// <summary>
/// Middleware to log all API requests with user info and response status
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Start timing
        var stopwatch = Stopwatch.StartNew();

        // Get request info
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            // Get user info (after authentication middleware has run)
            var userId = context.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirst("sub")?.Value
                  ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? context.User.Identity.Name
                  ?? "Authenticated"
                : "Anonymous";

            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Log format: [User] METHOD /path → StatusCode (Xms)
            var logLevel = statusCode >= 500 ? LogLevel.Error
                         : statusCode >= 400 ? LogLevel.Warning
                         : LogLevel.Information;

            _logger.Log(
                logLevel,
                "[{User}] {Method} {Path}{Query} → {StatusCode} ({ElapsedMs}ms)",
                userId,
                method,
                path,
                queryString,
                statusCode,
                elapsedMs
            );
        }
    }
}

/// <summary>
/// Extension method to register the request logging middleware
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
