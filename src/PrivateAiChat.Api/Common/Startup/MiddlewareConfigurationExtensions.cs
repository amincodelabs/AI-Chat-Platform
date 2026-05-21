using Microsoft.AspNetCore.HttpOverrides;
using PrivateAiChat.Api.Middleware;

namespace PrivateAiChat.Api.Common.Startup;

public static class MiddlewareConfigurationExtensions
{
    public static IServiceCollection AddForwardedHeadersConfiguration(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        return services;
    }

    public static WebApplication UseAppMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "PrivateAiChat API v1");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseForwardedHeaders();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<RequestCorrelationMiddleware>();
        app.UseCors("ConfiguredOrigins");
        app.UseAuthentication();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<ApiExceptionHandlingMiddleware>();
        app.UseStatusCodePages(async statusCodeContext =>
            await ErrorResponseHelpers.WriteStatusCodeErrorAsync(statusCodeContext.HttpContext));
        app.UseAuthorization();
        app.UseRateLimiter();

        return app;
    }
}
