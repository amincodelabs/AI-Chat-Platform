namespace PrivateAiChat.Api.Middleware;

public sealed class RequestCorrelationMiddleware
{
    public const string HeaderName = "X-Request-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCorrelationMiddleware> _logger;

    public RequestCorrelationMiddleware(
        RequestDelegate next,
        ILogger<RequestCorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = GetRequestId(context);
        context.TraceIdentifier = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId
        }))
        {
            await _next(context);
        }
    }

    private static string GetRequestId(HttpContext context)
    {
        var incomingRequestId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(incomingRequestId))
        {
            return context.TraceIdentifier;
        }

        var requestId = incomingRequestId.Trim();
        return requestId.Length <= 128 ? requestId : requestId[..128];
    }
}
