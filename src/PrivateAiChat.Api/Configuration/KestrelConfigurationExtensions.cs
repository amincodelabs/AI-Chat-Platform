namespace PrivateAiChat.Api.Configuration;

public static class KestrelConfigurationExtensions
{
    public static ConfigureWebHostBuilder ConfigureKestrel(this ConfigureWebHostBuilder webHost, IConfiguration configuration)
    {
        webHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize =
                configuration.GetValue<long?>("RequestLimits:MaxRequestBodyBytes") ?? 1_048_576;
        });

        return webHost;
    }
}
