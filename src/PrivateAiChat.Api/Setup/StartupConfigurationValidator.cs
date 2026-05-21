using PrivateAiChat.Infrastructure.Chat;

namespace PrivateAiChat.Api.Setup;

public static class StartupConfigurationValidator
{
    public static void Validate(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var databaseConnectionString = configuration.GetConnectionString("DefaultConnection");
        ValidateRequiredValue(databaseConnectionString, "ConnectionStrings:DefaultConnection");

        if (environment.IsProduction() &&
            databaseConnectionString!.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Production configuration must not use a LocalDB connection string.");
        }

        ValidateOllama(configuration);
        ValidateRedis(configuration);
        ValidateCors(configuration, environment);
        ValidateRequestLimits(configuration);
        ValidateMigrationSafety(configuration, environment);
        ValidateDevelopmentSeed(configuration, environment);
    }

    private static void ValidateOllama(IConfiguration configuration)
    {
        var section = configuration.GetSection(OllamaOptions.SectionName);
        var baseUrl = section[nameof(OllamaOptions.BaseUrl)];
        var model = section[nameof(OllamaOptions.Model)];
        var timeoutSeconds = section.GetValue<int?>(nameof(OllamaOptions.TimeoutSeconds));

        ValidateRequiredValue(baseUrl, $"{OllamaOptions.SectionName}:{nameof(OllamaOptions.BaseUrl)}");
        ValidateRequiredValue(model, $"{OllamaOptions.SectionName}:{nameof(OllamaOptions.Model)}");

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Configuration value 'Ollama:BaseUrl' must be an absolute HTTP or HTTPS URL.");
        }

        if (timeoutSeconds is null or <= 0)
        {
            throw new InvalidOperationException("Configuration value 'Ollama:TimeoutSeconds' must be greater than zero.");
        }
    }

    private static void ValidateRedis(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("Redis:Required"))
        {
            return;
        }

        ValidateRequiredValue(
            configuration.GetConnectionString("Redis"),
            "ConnectionStrings:Redis");
    }

    private static void ValidateMigrationSafety(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var applyMigrations = configuration.GetValue<bool>("Database:ApplyMigrations");
        var allowProductionAutoMigrations = configuration.GetValue<bool>("Database:AllowProductionAutoMigrations");

        if (environment.IsProduction() && applyMigrations && !allowProductionAutoMigrations)
        {
            throw new InvalidOperationException(
                "Database:ApplyMigrations is enabled in Production. Set Database:AllowProductionAutoMigrations=true only if this is intentional.");
        }
    }

    private static void ValidateCors(IConfiguration configuration, IWebHostEnvironment environment)
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

        foreach (var origin in origins.Where(origin => !string.IsNullOrWhiteSpace(origin)))
        {
            if (origin.Contains('*', StringComparison.Ordinal))
            {
                throw new InvalidOperationException("CORS allowed origins must not contain wildcards.");
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException($"CORS allowed origin '{origin}' must be an absolute HTTP or HTTPS origin.");
            }

            if (environment.IsProduction() && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException("Production CORS allowed origins must use HTTPS.");
            }
        }
    }

    private static void ValidateRequestLimits(IConfiguration configuration)
    {
        var maxRequestBodyBytes = configuration.GetValue<long?>("RequestLimits:MaxRequestBodyBytes");
        if (maxRequestBodyBytes is not null and <= 0)
        {
            throw new InvalidOperationException("Configuration value 'RequestLimits:MaxRequestBodyBytes' must be greater than zero.");
        }
    }

    private static void ValidateDevelopmentSeed(IConfiguration configuration, IWebHostEnvironment environment)
    {
        if (!configuration.GetValue<bool>("DevelopmentSeed:Enabled"))
        {
            return;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("DevelopmentSeed:Enabled can only be used when ASPNETCORE_ENVIRONMENT is Development.");
        }

        ValidateRequiredValue(
            configuration["DevelopmentSeed:TestUser:Email"],
            "DevelopmentSeed:TestUser:Email");
        ValidateRequiredValue(
            configuration["DevelopmentSeed:TestUser:Password"],
            "DevelopmentSeed:TestUser:Password");
    }

    private static void ValidateRequiredValue(string? value, string configurationKey)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required configuration value '{configurationKey}'.");
        }
    }
}
