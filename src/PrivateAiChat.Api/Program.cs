using Microsoft.EntityFrameworkCore;
using PrivateAiChat.Api.Common.Startup;
using PrivateAiChat.Api.Configuration;
using PrivateAiChat.Api.Endpoints;
using PrivateAiChat.Api.Setup;
using PrivateAiChat.Application.DependencyInjection;
using PrivateAiChat.Infrastructure.DependencyInjection;
using PrivateAiChat.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

StartupConfigurationValidator.Validate(builder.Configuration, builder.Environment);

builder.WebHost.ConfigureKestrel(builder.Configuration);

builder.Services.AddOpenApi();
builder.Services.AddForwardedHeadersConfiguration();
builder.Services.AddAppCors(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddAppHttpClients();
builder.Services.AddAppHealthChecks();
builder.Services.AddAppRateLimiting(builder.Configuration);
builder.Services.AddAppDataProtection();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

await DevelopmentDataSeeder.SeedAsync(app.Services);

app.UseAppMiddleware();

app.MapAuthEndpoints();
app.MapConversationEndpoints();
app.MapHealthEndpoints();

app.Run();
