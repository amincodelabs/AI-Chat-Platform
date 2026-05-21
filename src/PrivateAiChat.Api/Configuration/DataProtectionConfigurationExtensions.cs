using Microsoft.AspNetCore.DataProtection;

namespace PrivateAiChat.Api.Configuration;

public static class DataProtectionConfigurationExtensions
{
    public static IServiceCollection AddAppDataProtection(this IServiceCollection services)
    {
        var dataProtectionKeysPath = "/home/app/.aspnet/DataProtection-Keys";
        Directory.CreateDirectory(dataProtectionKeysPath);

        services
            .AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
            .SetApplicationName("PrivateAiChat");

        return services;
    }
}
