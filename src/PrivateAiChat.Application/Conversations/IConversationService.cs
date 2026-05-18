using PrivateAiChat.Contracts.Conversations;

namespace PrivateAiChat.Application.Conversations;

public interface IConversationService
{
    Task<ConversationSummaryResponse> CreateConversationAsync(
        Guid userId,
        CreateConversationRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ConversationSummaryResponse>> GetUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<ConversationDetailsResponse?> GetConversationDetailsAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<bool> DeleteConversationAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<MessageResponse?> AddMessageAsync(
        Guid userId,
        Guid conversationId,
        AddMessageRequest request,
        CancellationToken cancellationToken);
}
