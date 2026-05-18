namespace PrivateAiChat.Contracts.Conversations;

public sealed record ConversationSummaryResponse(
    Guid Id,
    string? Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
