using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PrivateAiChat.Api.Health;

public sealed class DistributedCacheHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;

    public DistributedCacheHealthCheck(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var key = $"health:{Guid.NewGuid():N}";

        try
        {
            await _cache.SetStringAsync(
                key,
                "ok",
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                },
                cancellationToken);

            var value = await _cache.GetStringAsync(key, cancellationToken);
            await _cache.RemoveAsync(key, cancellationToken);

            return value == "ok"
                ? HealthCheckResult.Healthy("Distributed cache is available.")
                : HealthCheckResult.Unhealthy("Distributed cache read/write check failed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Distributed cache is unavailable.", exception);
        }
    }
}
