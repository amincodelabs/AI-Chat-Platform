using System.ComponentModel.DataAnnotations;

namespace PrivateAiChat.Contracts.Auth;

public sealed record LoginRequest(
    [property: Required]
    [property: EmailAddress]
    [property: MaxLength(256)]
    string Email,

    [property: Required]
    [property: MinLength(8)]
    [property: MaxLength(128)]
    string Password);
