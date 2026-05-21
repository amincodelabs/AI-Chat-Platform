namespace PrivateAiChat.Api.Configuration;

public static class CorsConfigurationExtensions
{
    private const string ConfiguredOriginsPolicy = "ConfiguredOrigins";

    public static IServiceCollection AddAppCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(ConfiguredOriginsPolicy, policy =>
            {
                var allowedOrigins = GetAllowedOrigins(configuration);

                if (allowedOrigins.Length > 0)
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
            });
        });

        return services;
    }

    private static string[] GetAllowedOrigins(IConfiguration configuration)
    {
        var origins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        var originsCsv = configuration["Cors:AllowedOriginsCsv"];
        if (!string.IsNullOrWhiteSpace(originsCsv))
        {
            origins = origins
                .Concat(originsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .ToArray();
        }

        return origins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
