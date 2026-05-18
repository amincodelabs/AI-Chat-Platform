namespace PrivateAiChat.Contracts.Conversations;

public sealed record MessageResponse(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt);
