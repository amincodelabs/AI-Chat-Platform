using System.Diagnostics;
using System.Security.Claims;

namespace PrivateAiChat.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        var requestPath = context.Request.Path.Value ?? "/";
        var requestMethod = context.Request.Method;
        var requestId = context.TraceIdentifier;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            await _next(context);
        }
        finally
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp);
            var statusCode = context.Response.StatusCode;
            var logLevel = GetLogLevel(requestPath, statusCode);

            if (_logger.IsEnabled(logLevel))
            {
                _logger.Log(
                    logLevel,
                    "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {DurationMs} ms. CorrelationId={CorrelationId} UserId={UserId}",
                    requestMethod,
                    requestPath,
                    statusCode,
                    Math.Round(duration.TotalMilliseconds, 2),
                    requestId,
                    userId);
            }
        }
    }

    private static LogLevel GetLogLevel(string requestPath, int statusCode)
    {
        if (requestPath.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            return LogLevel.Debug;
        }

        return statusCode >= StatusCodes.Status500InternalServerError
            ? LogLevel.Error
            : statusCode >= StatusCodes.Status400BadRequest
                ? LogLevel.Warning
                : LogLevel.Information;
    }
}
