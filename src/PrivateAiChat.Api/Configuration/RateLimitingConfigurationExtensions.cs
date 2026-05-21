using System.Threading.RateLimiting;
using PrivateAiChat.Api.Common.Startup;
using PrivateAiChat.Api.RateLimiting;

namespace PrivateAiChat.Api.Configuration;

public static class RateLimitingConfigurationExtensions
{
    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(rateLimiterOptions =>
        {
            var authOptions = RateLimitHelpers.GetRateLimitOptions(configuration, "RateLimiting:Auth", 5, 60);
            var chatOptions = RateLimitHelpers.GetRateLimitOptions(configuration, "RateLimiting:Chat", 20, 60);
            var generalOptions = RateLimitHelpers.GetRateLimitOptions(configuration, "RateLimiting:General", 120, 60);

            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var retryAfter = RateLimitHelpers.GetRetryAfterSeconds(context.Lease);

                if (retryAfter is not null)
                {
                    httpContext.Response.Headers.RetryAfter = retryAfter.Value.ToString();
                }

                await ErrorResponseHelpers.WriteErrorAsync(
                    httpContext,
                    StatusCodes.Status429TooManyRequests,
                    "rate_limit_exceeded",
                    "Too many requests. Please wait before trying again.",
                    cancellationToken);
            };

            rateLimiterOptions.AddPolicy(RateLimitPolicyNames.Auth, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"auth:{RateLimitHelpers.GetClientPartition(httpContext, preferUser: false)}",
                    _ => RateLimitHelpers.ToFixedWindowOptions(authOptions)));

            rateLimiterOptions.AddPolicy(RateLimitPolicyNames.Chat, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"chat:{RateLimitHelpers.GetClientPartition(httpContext, preferUser: true)}",
                    _ => RateLimitHelpers.ToFixedWindowOptions(chatOptions)));

            rateLimiterOptions.AddPolicy(RateLimitPolicyNames.General, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"general:{RateLimitHelpers.GetClientPartition(httpContext, preferUser: true)}",
                    _ => RateLimitHelpers.ToFixedWindowOptions(generalOptions)));
        });

        return services;
    }
}
