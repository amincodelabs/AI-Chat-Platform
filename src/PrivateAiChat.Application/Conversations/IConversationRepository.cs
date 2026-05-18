using PrivateAiChat.Domain.Conversations;
using PrivateAiChat.Domain.Messages;

namespace PrivateAiChat.Application.Conversations;

public interface IConversationRepository
{
    Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Conversation>> GetUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Conversation?> GetUserConversationWithMessagesAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<Conversation?> GetUserConversationAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task AddMessageAsync(Message message, CancellationToken cancellationToken);

    void DeleteConversation(Conversation conversation);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
