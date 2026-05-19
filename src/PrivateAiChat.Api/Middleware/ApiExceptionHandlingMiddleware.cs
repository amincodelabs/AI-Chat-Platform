using PrivateAiChat.Application.Chat;
using PrivateAiChat.Contracts.Errors;

namespace PrivateAiChat.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request was cancelled by the client.");
        }
        catch (ChatCompletionException exception)
        {
            _logger.LogWarning(
                exception,
                "Chat completion failed with code {ErrorCode}.",
                exception.Code);

            await WriteErrorAsync(
                context,
                StatusCodes.Status502BadGateway,
                exception.Code,
                ToSafeChatMessage(exception));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception.");

            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "server_error",
                "An unexpected server error occurred.");
        }
    }

    private static string ToSafeChatMessage(ChatCompletionException exception) =>
        exception.Code switch
        {
            "ollama_unavailable" => "The AI service is currently unavailable.",
            "ollama_timeout" => "The AI service timed out. Please try again.",
            "ollama_http_error" => "The AI service returned an error.",
            "ollama_invalid_response" => "The AI service returned an invalid response.",
            _ => "The AI response could not be completed."
        };

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
            code,
            message,
            context.TraceIdentifier));
    }
}
