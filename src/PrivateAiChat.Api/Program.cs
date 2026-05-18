using PrivateAiChat.Infrastructure.DependencyInjection;
using PrivateAiChat.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/health", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var databaseConnected = await dbContext.Database.CanConnectAsync(cancellationToken);

    return Results.Ok(new
    {
        status = databaseConnected ? "Healthy" : "Degraded",
        database = databaseConnected ? "Connected" : "Unavailable"
    });
});

app.Run();
