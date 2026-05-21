using System.Security.Claims;
using System.Threading.RateLimiting;
using PrivateAiChat.Api.RateLimiting;

namespace PrivateAiChat.Api.Common.Startup;

public static class RateLimitHelpers
{
    public static RateLimitPolicyOptions GetRateLimitOptions(
        IConfiguration configuration,
        string sectionName,
        int defaultPermitLimit,
        int defaultWindowSeconds)
    {
        var options = new RateLimitPolicyOptions
        {
            PermitLimit = defaultPermitLimit,
            WindowSeconds = defaultWindowSeconds
        };

        configuration.GetSection(sectionName).Bind(options);
        options.PermitLimit = Math.Max(1, options.PermitLimit);
        options.WindowSeconds = Math.Max(1, options.WindowSeconds);

        return options;
    }

    public static FixedWindowRateLimiterOptions ToFixedWindowOptions(RateLimitPolicyOptions options) =>
        new()
        {
            PermitLimit = options.PermitLimit,
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        };

    public static string GetClientPartition(HttpContext httpContext, bool preferUser)
    {
        if (preferUser)
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                return $"user:{userId}";
            }
        }

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var ipAddress = forwardedFor?.Split(',').FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        }

        return string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : $"ip:{ipAddress}";
    }

    public static int? GetRetryAfterSeconds(RateLimitLease lease)
    {
        if (!lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return null;
        }

        return Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
    }
}
