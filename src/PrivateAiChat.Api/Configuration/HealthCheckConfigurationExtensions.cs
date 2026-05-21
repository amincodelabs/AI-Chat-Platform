using Microsoft.Extensions.Diagnostics.HealthChecks;
using PrivateAiChat.Api.Health;

namespace PrivateAiChat.Api.Configuration;

public static class HealthCheckConfigurationExtensions
{
    public static IServiceCollection AddAppHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("api", () => HealthCheckResult.Healthy("API process is running."), tags: ["live", "ready"])
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<DistributedCacheHealthCheck>("redis", tags: ["ready"])
            .AddCheck<OllamaHealthCheck>("ollama", tags: ["ready"]);

        return services;
    }
}
