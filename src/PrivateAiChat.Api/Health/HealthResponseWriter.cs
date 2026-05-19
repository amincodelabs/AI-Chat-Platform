using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PrivateAiChat.Api.Health;

public static class HealthResponseWriter
{
    public static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            requestId = context.TraceIdentifier,
            durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                    description = entry.Value.Description
                })
        };

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            context.RequestAborted);
    }
}
