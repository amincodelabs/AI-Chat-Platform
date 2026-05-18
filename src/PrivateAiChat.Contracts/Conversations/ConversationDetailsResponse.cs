namespace PrivateAiChat.Contracts.Conversations;

public sealed record ConversationDetailsResponse(
    Guid Id,
    string? Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<MessageResponse> Messages);
