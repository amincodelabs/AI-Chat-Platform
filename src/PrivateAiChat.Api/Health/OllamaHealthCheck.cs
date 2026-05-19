using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PrivateAiChat.Api.Health;

public sealed class OllamaHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OllamaHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("OllamaHealth");
            using var response = await httpClient.GetAsync("api/version", cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Ollama is reachable.")
                : HealthCheckResult.Unhealthy($"Ollama returned HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Ollama is unavailable.", exception);
        }
    }
}
