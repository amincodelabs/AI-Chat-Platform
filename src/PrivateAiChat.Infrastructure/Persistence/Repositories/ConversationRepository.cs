using Microsoft.EntityFrameworkCore;
using PrivateAiChat.Application.Conversations;
using PrivateAiChat.Domain.Conversations;
using PrivateAiChat.Domain.Messages;

namespace PrivateAiChat.Infrastructure.Persistence.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _dbContext;

    public ConversationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken) =>
        await _dbContext.Conversations.AddAsync(conversation, cancellationToken);

    public async Task<IReadOnlyCollection<Conversation>> GetUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await _dbContext.Conversations
            .Where(conversation => conversation.UserId == userId)
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .ToArrayAsync(cancellationToken);

    public async Task<Conversation?> GetUserConversationWithMessagesAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken) =>
        await _dbContext.Conversations
            .Include(conversation => conversation.Messages)
            .SingleOrDefaultAsync(
                conversation => conversation.Id == conversationId && conversation.UserId == userId,
                cancellationToken);

    public async Task<Conversation?> GetUserConversationAsync(
        Guid userId,
        Guid conversationId,
        CancellationToken cancellationToken) =>
        await _dbContext.Conversations
            .SingleOrDefaultAsync(
                conversation => conversation.Id == conversationId && conversation.UserId == userId,
                cancellationToken);

    public async Task AddMessageAsync(Message message, CancellationToken cancellationToken) =>
        await _dbContext.Messages.AddAsync(message, cancellationToken);

    public void DeleteConversation(Conversation conversation) =>
        _dbContext.Conversations.Remove(conversation);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
