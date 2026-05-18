using PrivateAiChat.Domain.Common;

namespace PrivateAiChat.Domain.Messages;

public sealed class Message : AuditableEntity
{
    private Message()
    {
    }

    public Message(Guid conversationId, MessageRole role, string content) : this()
    {
        ConversationId = conversationId == Guid.Empty
            ? throw new ArgumentException("Conversation id is required.", nameof(conversationId))
            : conversationId;

        Role = role;
        Content = string.IsNullOrWhiteSpace(content)
            ? throw new ArgumentException("Content is required.", nameof(content))
            : content.Trim();
    }

    public Guid ConversationId { get; private set; }

    public MessageRole Role { get; private set; }

    public string Content { get; private set; } = string.Empty;
}
