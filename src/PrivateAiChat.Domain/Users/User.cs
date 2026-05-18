using Microsoft.AspNetCore.Identity;
using PrivateAiChat.Domain.Conversations;

namespace PrivateAiChat.Domain.Users;

public sealed class User : IdentityUser<Guid>
{
    private readonly List<Conversation> _conversations = new();

    private User()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public User(string email, string? displayName = null) : this()
    {
        Email = string.IsNullOrWhiteSpace(email)
            ? throw new ArgumentException("Email is required.", nameof(email))
            : email.Trim();

        UserName = Email;
        DisplayName = displayName?.Trim();
    }

    public string? DisplayName { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public IReadOnlyCollection<Conversation> Conversations => _conversations;

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DeletedAt.Value;
    }
}
