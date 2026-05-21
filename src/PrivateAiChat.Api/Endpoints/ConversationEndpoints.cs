using System.Security.Claims;
using PrivateAiChat.Api.Common.Startup;
using PrivateAiChat.Api.RateLimiting;
using PrivateAiChat.Application.Chat;
using PrivateAiChat.Application.Conversations;
using PrivateAiChat.Contracts.Conversations;

namespace PrivateAiChat.Api.Endpoints;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var conversations = app.MapGroup("/api/conversations")
            .RequireAuthorization();

        conversations.MapPost("/", async (
            CreateConversationRequest request,
            ClaimsPrincipal principal,
            IConversationService conversationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!AuthHelpers.TryGetUserId(principal, out var userId))
            {
                return ErrorResponseHelpers.UnauthorizedError(httpContext);
            }

            if (!ValidationHelpers.TryValidate(request, out var validationErrors))
            {
                return ErrorResponseHelpers.ValidationError(httpContext, validationErrors);
            }

            var conversation = await conversationService.CreateConversationAsync(
                userId,
                request,
                cancellationToken);

            return Results.Created($"/api/conversations/{conversation.Id}", conversation);
        })
        .RequireRateLimiting(RateLimitPolicyNames.Chat);

        conversations.MapGet("/", async (
            ClaimsPrincipal principal,
            IConversationService conversationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!AuthHelpers.TryGetUserId(principal, out var userId))
            {
                return ErrorResponseHelpers.UnauthorizedError(httpContext);
            }

            var userConversations = await conversationService.GetUserConversationsAsync(
                userId,
                cancellationToken);

            return Results.Ok(userConversations);
        })
        .RequireRateLimiting(RateLimitPolicyNames.General);

        conversations.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            IConversationService conversationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!AuthHelpers.TryGetUserId(principal, out var userId))
            {
                return ErrorResponseHelpers.UnauthorizedError(httpContext);
            }

            var conversation = await conversationService.GetConversationDetailsAsync(
                userId,
                id,
                cancellationToken);

            return conversation is null
                ? ErrorResponseHelpers.NotFoundError(httpContext, "Conversation was not found.")
                : Results.Ok(conversation);
        })
        .RequireRateLimiting(RateLimitPolicyNames.General);

        conversations.MapPut("/{id:guid}", async (
            Guid id,
            RenameConversationRequest request,
            ClaimsPrincipal principal,
            IConversationService conversationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!AuthHelpers.TryGetUserId(principal, out var userId))
            {
                return ErrorResponseHelpers.UnauthorizedError(httpContext);
            }

            if (!ValidationHelpers.TryValidate(request, out var validationErrors))
            {
                return ErrorResponseHelpers.ValidationError(httpContext, validationErrors);
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return ErrorResponseHelpers.ValidationError(
                    httpContext,
                    new Dictionary<string, string[]>
                    {
                        [nameof(RenameConversationRequest.Title)] = ["Title is required."]
                    });
            }

            var conversation = await conversationService.RenameConversationAsync(
                userId,
                id,
                request,
                cancellationToken);

            return conversation is null
                ? ErrorResponseHelpers.NotFoundError(httpContext, "Conversation was not found.")
                : Results.Ok(conversation);
        })
        .RequireRateLimiting(RateLimitPolicyNames.General);

        conversations.MapDelete("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal principal,
            IConversationService conversationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!AuthHelpers.TryGetUserId(principal, out var userId))
            {
                return ErrorResponseHelpers.UnauthorizedError(httpContext);
            }

            var deleted = await conversationService.DeleteConversationAsync(
                userId,
                id,
                cancellationToken);

            return deleted
                ? Results.NoContent()
                : ErrorResponseHelpers.NotFoundError(httpContext, "Conversation was not found.");
        })
        .RequireRateLimiting(RateLimitPolicyNames.General);

        conversations.MapPost("/{id:guid}/messages", async (
            Guid id,
            AddMessageRequest request,
            ClaimsPrincipal principal,
            IConversationService conversationService,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!AuthHelpers.TryGetUserId(principal, out var userId))
            {
                return ErrorResponseHelpers.UnauthorizedError(httpContext);
            }

            if (!ValidationHelpers.TryValidate(request, out var validationErrors))
            {
                return ErrorResponseHelpers.ValidationError(httpContext, validationErrors);
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return ErrorResponseHelpers.ValidationError(
                    httpContext,
                    new Dictionary<string, string[]>
                    {
                        [nameof(AddMessageRequest.Content)] = ["Message content is required."]
                    });
            }

            var messages = await conversationService.AddMessageAsync(
                userId,
                id,
                request,
                cancellationToken);

            return messages is null
                ? ErrorResponseHelpers.NotFoundError(httpContext, "Conversation was not found.")
                : Results.Ok(messages);
        })
        .RequireRateLimiting(RateLimitPolicyNames.Chat);

        conversations.MapPost("/{id:guid}/messages/stream", async (
            Guid id,
            AddMessageRequest request,
            ClaimsPrincipal principal,
            IConversationService conversationService,
            ILogger<Program> logger,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (!AuthHelpers.TryGetUserId(principal, out var userId))
            {
                await ErrorResponseHelpers.WriteErrorAsync(
                    httpContext,
                    StatusCodes.Status401Unauthorized,
                    "unauthorized",
                    "Authentication is required.",
                    cancellationToken);
                return;
            }

            if (!ValidationHelpers.TryValidate(request, out var validationErrors))
            {
                await ErrorResponseHelpers.WriteErrorAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "validation_failed",
                    "One or more validation errors occurred.",
                    cancellationToken,
                    validationErrors);
                return;
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                await ErrorResponseHelpers.WriteErrorAsync(
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "validation_failed",
                    "One or more validation errors occurred.",
                    cancellationToken,
                    new Dictionary<string, string[]>
                    {
                        [nameof(AddMessageRequest.Content)] = ["Message content is required."]
                    });
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
                        await ServerSentEventHelpers.WriteServerSentEventAsync(
                            httpContext.Response,
                            new ChatStreamEvent(ChatStreamEvent.Error, Content: "Conversation was not found."),
                            httpContext.RequestAborted);
                        return;
                    }

                    await ServerSentEventHelpers.WriteServerSentEventAsync(
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

                await ServerSentEventHelpers.WriteServerSentEventAsync(
                    httpContext.Response,
                    new ChatStreamEvent(ChatStreamEvent.Error, Content: ErrorResponseHelpers.ToSafeChatMessage(exception)),
                    cancellationToken);
            }
        })
        .RequireRateLimiting(RateLimitPolicyNames.Chat);

        return app;
    }
}
