namespace PrivateAiChat.Application.Chat;

public sealed record ChatCompletionMessage(
    string Role,
    string Content);
