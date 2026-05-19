namespace PrivateAiChat.Contracts.Errors;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    string RequestId,
    IReadOnlyDictionary<string, string[]>? Errors = null);
