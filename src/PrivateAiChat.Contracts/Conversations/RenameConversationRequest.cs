using System.ComponentModel.DataAnnotations;

namespace PrivateAiChat.Contracts.Conversations;

public sealed record RenameConversationRequest(
    [property: Required]
    [property: MaxLength(200)]
    string Title);
