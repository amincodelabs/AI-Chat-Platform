using Microsoft.Extensions.Diagnostics.HealthChecks;
using PrivateAiChat.Infrastructure.Persistence;

namespace PrivateAiChat.Api.Health;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _dbContext;

    public DatabaseHealthCheck(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database connection failed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database is unavailable.", exception);
        }
    }
}
