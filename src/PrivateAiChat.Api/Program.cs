using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using PrivateAiChat.Application.Chat;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PrivateAiChat.Application.Conversations;
using PrivateAiChat.Application.DependencyInjection;
using PrivateAiChat.Contracts.Auth;
using PrivateAiChat.Contracts.Conversations;
using PrivateAiChat.Domain.Users;
using PrivateAiChat.Infrastructure.DependencyInjection;
using PrivateAiChat.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/signup", async (
    SignupRequest request,
    UserManager<User> userManager,
    SignInManager<User> signInManager) =>
{
    if (!TryValidate(request, out var validationErrors))
    {
        return Results.ValidationProblem(validationErrors);
    }

    var existingUser = await userManager.FindByEmailAsync(request.Email.Trim());
    if (existingUser is not null)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [nameof(SignupRequest.Email)] = ["Email is already registered."]
        });
    }

    var user = new User(request.Email, request.DisplayName);
    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        return Results.ValidationProblem(ToValidationErrors(result));
    }

    await signInManager.SignInAsync(user, isPersistent: true);

    return Results.Ok(ToAuthResponse(user));
})
.AllowAnonymous();

app.MapPost("/auth/login", async (
    LoginRequest request,
    UserManager<User> userManager,
    SignInManager<User> signInManager) =>
{
    if (!TryValidate(request, out var validationErrors))
    {
        return Results.ValidationProblem(validationErrors);
    }

    var user = await userManager.FindByEmailAsync(request.Email.Trim());
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var result = await signInManager.PasswordSignInAsync(
        user,
        request.Password,
        isPersistent: true,
        lockoutOnFailure: true);

    return result.Succeeded
        ? Results.Ok(ToAuthResponse(user))
        : Results.Unauthorized();
})
.AllowAnonymous();

app.MapPost("/auth/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.NoContent();
})
.RequireAuthorization();

var conversations = app.MapGroup("/api/conversations")
    .RequireAuthorization();

conversations.MapPost("/", async (
    CreateConversationRequest request,
    ClaimsPrincipal principal,
    IConversationService conversationService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    if (!TryValidate(request, out var validationErrors))
    {
        return Results.ValidationProblem(validationErrors);
    }

    var conversation = await conversationService.CreateConversationAsync(
        userId,
        request,
        cancellationToken);

    return Results.Created($"/api/conversations/{conversation.Id}", conversation);
});

conversations.MapGet("/", async (
    ClaimsPrincipal principal,
    IConversationService conversationService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    var userConversations = await conversationService.GetUserConversationsAsync(
        userId,
        cancellationToken);

    return Results.Ok(userConversations);
});

conversations.MapGet("/{id:guid}", async (
    Guid id,
    ClaimsPrincipal principal,
    IConversationService conversationService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    var conversation = await conversationService.GetConversationDetailsAsync(
        userId,
        id,
        cancellationToken);

    return conversation is null ? Results.NotFound() : Results.Ok(conversation);
});

conversations.MapDelete("/{id:guid}", async (
    Guid id,
    ClaimsPrincipal principal,
    IConversationService conversationService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    var deleted = await conversationService.DeleteConversationAsync(
        userId,
        id,
        cancellationToken);

    return deleted ? Results.NoContent() : Results.NotFound();
});

conversations.MapPost("/{id:guid}/messages", async (
    Guid id,
    AddMessageRequest request,
    ClaimsPrincipal principal,
    IConversationService conversationService,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return Results.Unauthorized();
    }

    if (!TryValidate(request, out var validationErrors))
    {
        return Results.ValidationProblem(validationErrors);
    }

    try
    {
        var messages = await conversationService.AddMessageAsync(
            userId,
            id,
            request,
            cancellationToken);

        return messages is null ? Results.NotFound() : Results.Ok(messages);
    }
    catch (ChatCompletionException exception)
    {
        return Results.Problem(
            title: "Chat completion failed.",
            detail: exception.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapGet("/health", async (
    AppDbContext dbContext,
    IDistributedCache distributedCache,
    CancellationToken cancellationToken) =>
{
    var databaseConnected = await dbContext.Database.CanConnectAsync(cancellationToken);
    var cacheConnected = await CanUseCacheAsync(distributedCache, cancellationToken);

    return Results.Ok(new
    {
        status = databaseConnected && cacheConnected ? "Healthy" : "Degraded",
        database = databaseConnected ? "Connected" : "Unavailable",
        cache = cacheConnected ? "Connected" : "Unavailable"
    });
});

app.Run();

static AuthResponse ToAuthResponse(User user) =>
    new(
        user.Id,
        user.Email ?? string.Empty,
        user.DisplayName);

static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
{
    var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    return Guid.TryParse(userIdValue, out userId);
}

static bool TryValidate<TRequest>(
    TRequest request,
    out Dictionary<string, string[]> validationErrors)
{
    var results = new List<ValidationResult>();
    var context = new ValidationContext(request!);

    var isValid = Validator.TryValidateObject(request!, context, results, validateAllProperties: true);
    validationErrors = results
        .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
            .Select(memberName => new { memberName, result.ErrorMessage }))
        .GroupBy(error => error.memberName)
        .ToDictionary(
            group => group.Key,
            group => group
                .Select(error => error.ErrorMessage ?? "The request is invalid.")
                .ToArray());

    return isValid;
}

static Dictionary<string, string[]> ToValidationErrors(IdentityResult result) =>
    result.Errors
        .GroupBy(error => error.Code)
        .ToDictionary(
            group => group.Key,
            group => group.Select(error => error.Description).ToArray());

static async Task<bool> CanUseCacheAsync(
    IDistributedCache distributedCache,
    CancellationToken cancellationToken)
{
    try
    {
        var key = $"health:{Guid.NewGuid():N}";
        await distributedCache.SetStringAsync(
            key,
            "ok",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            },
            cancellationToken);

        return await distributedCache.GetStringAsync(key, cancellationToken) == "ok";
    }
    catch
    {
        return false;
    }
}
