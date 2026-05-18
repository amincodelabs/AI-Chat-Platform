namespace PrivateAiChat.Contracts.Conversations;

public sealed record AddMessageResponse(
    MessageResponse UserMessage,
    MessageResponse AssistantMessage);
