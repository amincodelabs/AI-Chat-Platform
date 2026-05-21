using PrivateAiChat.Application.Chat;
using PrivateAiChat.Contracts.Errors;

namespace PrivateAiChat.Api.Common.Startup;

public static class ErrorResponseHelpers
{
    public static IResult UnauthorizedError(HttpContext httpContext) =>
        ErrorResult(
            httpContext,
            StatusCodes.Status401Unauthorized,
            "unauthorized",
            "Authentication is required.");

    public static IResult NotFoundError(HttpContext httpContext, string message) =>
        ErrorResult(
            httpContext,
            StatusCodes.Status404NotFound,
            "not_found",
            message);

    public static IResult ValidationError(
        HttpContext httpContext,
        IReadOnlyDictionary<string, string[]> validationErrors) =>
        ErrorResult(
            httpContext,
            StatusCodes.Status400BadRequest,
            "validation_failed",
            "One or more validation errors occurred.",
            validationErrors);

    public static IResult ErrorResult(
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

    public static async Task WriteStatusCodeErrorAsync(HttpContext httpContext)
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

    public static async Task WriteErrorAsync(
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

    public static string ToSafeChatMessage(ChatCompletionException exception) =>
        exception.Code switch
        {
            "ollama_unavailable" => "The AI service is currently unavailable.",
            "ollama_timeout" => "The AI service timed out. Please try again.",
            "ollama_http_error" => "The AI service returned an error.",
            "ollama_invalid_response" => "The AI service returned an invalid response.",
            _ => "The AI response could not be completed."
        };
}
