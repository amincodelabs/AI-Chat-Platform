using PrivateAiChat.Domain.Common;
using PrivateAiChat.Domain.Messages;

namespace PrivateAiChat.Domain.Conversations;

public sealed class Conversation : AuditableEntity
{
    private readonly List<Message> _messages = new();

    private Conversation()
    {
    }

    public Conversation(Guid userId, string? title = null) : this()
    {
        UserId = userId == Guid.Empty
            ? throw new ArgumentException("User id is required.", nameof(userId))
            : userId;

        Title = title?.Trim();
    }

    public Guid UserId { get; private set; }

    public string? Title { get; private set; }

    public IReadOnlyCollection<Message> Messages => _messages;
}
