using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using PrivateAiChat.Api.Health;

namespace PrivateAiChat.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
            ResponseWriter = HealthResponseWriter.WriteAsync
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("ready"),
            ResponseWriter = HealthResponseWriter.WriteAsync
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = healthCheck => healthCheck.Tags.Contains("live"),
            ResponseWriter = HealthResponseWriter.WriteAsync
        });

        return app;
    }
}
