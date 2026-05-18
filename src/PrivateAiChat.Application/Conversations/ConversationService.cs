using PrivateAiChat.Application.Chat;
using PrivateAiChat.Contracts.Conversations;
using PrivateAiChat.Domain.Conversations;
using PrivateAiChat.Domain.Messages;

namespace PrivateAiChat.Application.Conversations;

public sealed class ConversationService : IConversationService
{
    private readonly IConversationRepository _repository;
    private readonly IChatCompletionService _chatCompletionService;

    public ConversationService(
        IConversationRepository repository,
        IChatCompletionService chatCompletionService)
    {
        _repository = repository;
        _chatCompletionService = chatCompletionService;
    }

    public async Task<ConversationSummaryResponse> CreateConversationAsync(
        Guid userId,
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = new Conversation(userId, request.Title);

        await _repository.AddConversationAsync(conversation, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return ToSummaryResponse(conversation);
    }

    public async Task<IReadOnlyCollection<ConversationSummaryResponse>> GetUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var conversations = await _repository.GetUserConversationsAsync(userId, cancellationToken);

        return conversations
            .Select(ToSummaryResponse)
            .ToArray();
    }

    public async Task<ConversationDetailsResponse?> GetConversationDetailsAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetUserConversationWithMessagesAsync(
            userId,
            conversationId,
            cancellationToken);

        return conversation is null ? null : ToDetailsResponse(conversation);
    }

    public async Task<bool> DeleteConversationAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetUserConversationAsync(
            userId,
            conversationId,
            cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        _repository.DeleteConversation(conversation);
        await _repository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<AddMessageResponse?> AddMessageAsync(
        Guid userId,
        Guid conversationId,
        AddMessageRequest request,
        CancellationToken cancellationToken)
    {
        var conversation = await _repository.GetUserConversationWithMessagesAsync(
            userId,
            conversationId,
            cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var message = new Message(conversation.Id, MessageRole.User, request.Content);
        conversation.Touch();

        await _repository.AddMessageAsync(message, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var history = conversation.Messages
            .OrderBy(existingMessage => existingMessage.CreatedAt)
            .Append(message)
            .Select(ToChatCompletionMessage)
            .ToArray();

        var assistantContent = await _chatCompletionService.CompleteAsync(history, cancellationToken);
        var assistantMessage = new Message(conversation.Id, MessageRole.Assistant, assistantContent);
        conversation.Touch();

        await _repository.AddMessageAsync(assistantMessage, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new AddMessageResponse(
            ToMessageResponse(message),
            ToMessageResponse(assistantMessage));
    }

    private static ConversationSummaryResponse ToSummaryResponse(Conversation conversation) =>
        new(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt);

    private static ConversationDetailsResponse ToDetailsResponse(Conversation conversation) =>
        new(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            conversation.Messages
                .OrderBy(message => message.CreatedAt)
                .Select(ToMessageResponse)
                .ToArray());

    private static MessageResponse ToMessageResponse(Message message) =>
        new(
            message.Id,
            message.Role.ToString(),
            message.Content,
            message.CreatedAt);

    private static ChatCompletionMessage ToChatCompletionMessage(Message message) =>
        new(
            message.Role.ToString().ToLowerInvariant(),
            message.Content);
}
