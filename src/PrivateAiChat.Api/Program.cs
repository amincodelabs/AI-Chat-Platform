using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using PrivateAiChat.Api.Middleware;
using PrivateAiChat.Application.Chat;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PrivateAiChat.Application.Conversations;
using PrivateAiChat.Application.DependencyInjection;
using PrivateAiChat.Contracts.Auth;
using PrivateAiChat.Contracts.Conversations;
using PrivateAiChat.Contracts.Errors;
using PrivateAiChat.Domain.Users;
using PrivateAiChat.Infrastructure.DependencyInjection;
using PrivateAiChat.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();

var dataProtectionKeysPath = "/home/app/.aspnet/DataProtection-Keys";
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("PrivateAiChat");

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

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.UseStatusCodePages(async statusCodeContext =>
    await WriteStatusCodeErrorAsync(statusCodeContext.HttpContext));
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/signup", async (
    SignupRequest request,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    HttpContext httpContext) =>
{
    if (!TryValidate(request, out var validationErrors))
    {
        return ValidationError(httpContext, validationErrors);
    }

    var existingUser = await userManager.FindByEmailAsync(request.Email.Trim());
    if (existingUser is not null)
    {
        return ValidationError(httpContext, new Dictionary<string, string[]>
        {
            [nameof(SignupRequest.Email)] = ["Email is already registered."]
        });
    }

    var user = new User(request.Email, request.DisplayName);
    var result = await userManager.CreateAsync(user, request.Password);

    if (!result.Succeeded)
    {
        return ValidationError(httpContext, ToValidationErrors(result));
    }

    await signInManager.SignInAsync(user, isPersistent: true);

    return Results.Ok(ToAuthResponse(user));
})
.AllowAnonymous();

app.MapPost("/auth/login", async (
    LoginRequest request,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    HttpContext httpContext) =>
{
    if (!TryValidate(request, out var validationErrors))
    {
        return ValidationError(httpContext, validationErrors);
    }

    var user = await userManager.FindByEmailAsync(request.Email.Trim());
    if (user is null)
    {
        return UnauthorizedError(httpContext);
    }

    var result = await signInManager.PasswordSignInAsync(
        user,
        request.Password,
        isPersistent: true,
        lockoutOnFailure: true);

    return result.Succeeded
        ? Results.Ok(ToAuthResponse(user))
        : UnauthorizedError(httpContext);
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
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return UnauthorizedError(httpContext);
    }

    if (!TryValidate(request, out var validationErrors))
    {
        return ValidationError(httpContext, validationErrors);
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
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return UnauthorizedError(httpContext);
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
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return UnauthorizedError(httpContext);
    }

    var conversation = await conversationService.GetConversationDetailsAsync(
        userId,
        id,
        cancellationToken);

    return conversation is null ? NotFoundError(httpContext, "Conversation was not found.") : Results.Ok(conversation);
});

conversations.MapDelete("/{id:guid}", async (
    Guid id,
    ClaimsPrincipal principal,
    IConversationService conversationService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return UnauthorizedError(httpContext);
    }

    var deleted = await conversationService.DeleteConversationAsync(
        userId,
        id,
        cancellationToken);

    return deleted ? Results.NoContent() : NotFoundError(httpContext, "Conversation was not found.");
});

conversations.MapPost("/{id:guid}/messages", async (
    Guid id,
    AddMessageRequest request,
    ClaimsPrincipal principal,
    IConversationService conversationService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        return UnauthorizedError(httpContext);
    }

    if (!TryValidate(request, out var validationErrors))
    {
        return ValidationError(httpContext, validationErrors);
    }

    var messages = await conversationService.AddMessageAsync(
        userId,
        id,
        request,
        cancellationToken);

    return messages is null ? NotFoundError(httpContext, "Conversation was not found.") : Results.Ok(messages);
});

conversations.MapPost("/{id:guid}/messages/stream", async (
    Guid id,
    AddMessageRequest request,
    ClaimsPrincipal principal,
    IConversationService conversationService,
    ILogger<Program> logger,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    if (!TryGetUserId(principal, out var userId))
    {
        await WriteErrorAsync(
            httpContext,
            StatusCodes.Status401Unauthorized,
            "unauthorized",
            "Authentication is required.",
            cancellationToken);
        return;
    }

    if (!TryValidate(request, out var validationErrors))
    {
        await WriteErrorAsync(
            httpContext,
            StatusCodes.Status400BadRequest,
            "validation_failed",
            "One or more validation errors occurred.",
            cancellationToken,
            validationErrors);
        return;
    }

    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";
    httpContext.Response.ContentType = "text/event-stream";

    try
    {
        await foreach (var streamEvent in conversationService.AddMessageStreamingAsync(
            userId,
            id,
            request,
            httpContext.RequestAborted))
        {
            if (streamEvent.Type == ChatStreamEvent.NotFound)
            {
                await WriteServerSentEventAsync(
                    httpContext.Response,
                    new ChatStreamEvent(ChatStreamEvent.Error, Content: "Conversation was not found."),
                    httpContext.RequestAborted);
                return;
            }

            await WriteServerSentEventAsync(
                httpContext.Response,
                streamEvent,
                httpContext.RequestAborted);
        }
    }
    catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
    {
        logger.LogInformation("Streaming chat request was cancelled by the client.");
    }
    catch (ChatCompletionException exception)
    {
        logger.LogWarning(
            exception,
            "Streaming chat completion failed with code {ErrorCode}.",
            exception.Code);

        await WriteServerSentEventAsync(
            httpContext.Response,
            new ChatStreamEvent(ChatStreamEvent.Error, Content: ToSafeChatMessage(exception)),
            cancellationToken);
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

static IResult UnauthorizedError(HttpContext httpContext) =>
    ErrorResult(
        httpContext,
        StatusCodes.Status401Unauthorized,
        "unauthorized",
        "Authentication is required.");

static IResult NotFoundError(HttpContext httpContext, string message) =>
    ErrorResult(
        httpContext,
        StatusCodes.Status404NotFound,
        "not_found",
        message);

static IResult ValidationError(
    HttpContext httpContext,
    IReadOnlyDictionary<string, string[]> validationErrors) =>
    ErrorResult(
        httpContext,
        StatusCodes.Status400BadRequest,
        "validation_failed",
        "One or more validation errors occurred.",
        validationErrors);

static IResult ErrorResult(
    HttpContext httpContext,
    int statusCode,
    string code,
    string message,
    IReadOnlyDictionary<string, string[]>? errors = null) =>
    Results.Json(
        new ApiErrorResponse(
            code,
            message,
            httpContext.TraceIdentifier,
            errors),
        statusCode: statusCode);

static async Task WriteStatusCodeErrorAsync(HttpContext httpContext)
{
    if (httpContext.Response.HasStarted || httpContext.Response.ContentLength > 0)
    {
        return;
    }

    var (code, message) = httpContext.Response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => ("unauthorized", "Authentication is required."),
        StatusCodes.Status403Forbidden => ("forbidden", "You do not have access to this resource."),
        StatusCodes.Status404NotFound => ("not_found", "The requested resource was not found."),
        _ => ("request_failed", "The request could not be completed.")
    };

    httpContext.Response.ContentType = "application/json";
    await httpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(
        code,
        message,
        httpContext.TraceIdentifier));
}

static async Task WriteErrorAsync(
    HttpContext httpContext,
    int statusCode,
    string code,
    string message,
    CancellationToken cancellationToken,
    IReadOnlyDictionary<string, string[]>? errors = null)
{
    httpContext.Response.StatusCode = statusCode;
    httpContext.Response.ContentType = "application/json";

    await httpContext.Response.WriteAsJsonAsync(
        new ApiErrorResponse(
            code,
            message,
            httpContext.TraceIdentifier,
            errors),
        cancellationToken);
}

static string ToSafeChatMessage(ChatCompletionException exception) =>
    exception.Code switch
    {
        "ollama_unavailable" => "The AI service is currently unavailable.",
        "ollama_timeout" => "The AI service timed out. Please try again.",
        "ollama_http_error" => "The AI service returned an error.",
        "ollama_invalid_response" => "The AI service returned an invalid response.",
        _ => "The AI response could not be completed."
    };

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

static async Task WriteServerSentEventAsync(
    HttpResponse response,
    ChatStreamEvent streamEvent,
    CancellationToken cancellationToken)
{
    var payload = JsonSerializer.Serialize(
        streamEvent,
        new JsonSerializerOptions(JsonSerializerDefaults.Web));

    await response.WriteAsync($"event: {streamEvent.Type}\n", cancellationToken);
    await response.WriteAsync($"data: {payload}\n\n", cancellationToken);
    await response.Body.FlushAsync(cancellationToken);
}
