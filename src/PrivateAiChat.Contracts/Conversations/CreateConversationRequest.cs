using System.ComponentModel.DataAnnotations;

namespace PrivateAiChat.Contracts.Conversations;

public sealed record CreateConversationRequest(
    [property: MaxLength(200)]
    string? Title);
