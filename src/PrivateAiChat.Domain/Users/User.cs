using PrivateAiChat.Domain.Common;
using PrivateAiChat.Domain.Conversations;

namespace PrivateAiChat.Domain.Users;

public sealed class User : AuditableEntity
{
    private readonly List<Conversation> _conversations = new();

    private User()
    {
    }

    public User(string email, string? displayName = null) : this()
    {
        Email = string.IsNullOrWhiteSpace(email)
            ? throw new ArgumentException("Email is required.", nameof(email))
            : email.Trim();

        DisplayName = displayName?.Trim();
    }

    public string Email { get; private set; } = string.Empty;

    public string? DisplayName { get; private set; }

    public IReadOnlyCollection<Conversation> Conversations => _conversations;
}
