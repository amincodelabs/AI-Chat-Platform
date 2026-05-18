using System.ComponentModel.DataAnnotations;

namespace PrivateAiChat.Contracts.Conversations;

public sealed record AddMessageRequest(
    [property: Required]
    [property: MaxLength(16000)]
    string Content);
