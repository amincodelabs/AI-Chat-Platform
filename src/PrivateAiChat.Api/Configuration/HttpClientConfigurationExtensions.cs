using PrivateAiChat.Infrastructure.Chat;

namespace PrivateAiChat.Api.Configuration;

public static class HttpClientConfigurationExtensions
{
    public static IServiceCollection AddAppHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient("OllamaHealth", (serviceProvider, client) =>
        {
            var options = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaOptions>>()
                .Value;

            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}
