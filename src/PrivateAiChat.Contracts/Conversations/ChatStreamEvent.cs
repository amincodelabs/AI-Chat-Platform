namespace PrivateAiChat.Contracts.Conversations;

public sealed record ChatStreamEvent(
    string Type,
    string? Content = null,
    MessageResponse? Message = null)
{
    public const string UserMessage = "userMessage";
    public const string AssistantChunk = "assistantChunk";
    public const string AssistantMessage = "assistantMessage";
    public const string Error = "error";
    public const string NotFound = "notFound";
}
