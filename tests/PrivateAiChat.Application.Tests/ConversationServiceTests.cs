using PrivateAiChat.Application.Conversations;
using PrivateAiChat.Contracts.Conversations;
using PrivateAiChat.Domain.Conversations;
using PrivateAiChat.Domain.Messages;
using Xunit;

namespace PrivateAiChat.Application.Tests;

public sealed class ConversationServiceTests
{
    [Fact]
    public async Task CreateConversationAsync_Creates_UserOwned_Conversation()
    {
        var repository = new FakeConversationRepository();
        var service = new ConversationService(repository);
        var userId = Guid.NewGuid();

        var result = await service.CreateConversationAsync(
            userId,
            new CreateConversationRequest(" Test chat "),
            CancellationToken.None);

        Assert.Equal("Test chat", result.Title);
        Assert.Single(repository.Conversations);
        Assert.Equal(userId, repository.Conversations.Single().UserId);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task GetConversationDetailsAsync_Returns_Null_When_Conversation_Is_Not_UserOwned()
    {
        var repository = new FakeConversationRepository();
        var service = new ConversationService(repository);

        var result = await service.GetConversationDetailsAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddMessageAsync_Adds_User_Message_When_Conversation_Is_UserOwned()
    {
        var repository = new FakeConversationRepository();
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Owned");
        repository.Conversations.Add(conversation);
        var service = new ConversationService(repository);

        var result = await service.AddMessageAsync(
            userId,
            conversation.Id,
            new AddMessageRequest(" Hello "),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(MessageRole.User.ToString(), result.Role);
        Assert.Equal("Hello", result.Content);
        Assert.Single(repository.Messages);
        Assert.Equal(conversation.Id, repository.Messages.Single().ConversationId);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    private sealed class FakeConversationRepository : IConversationRepository
    {
        public List<Conversation> Conversations { get; } = new();

        public List<Message> Messages { get; } = new();

        public int SaveChangesCount { get; private set; }

        public Task AddConversationAsync(Conversation conversation, CancellationToken cancellationToken)
        {
            Conversations.Add(conversation);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Conversation>> GetUserConversationsAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<Conversation>>(
                Conversations
                    .Where(conversation => conversation.UserId == userId)
                    .ToArray());

        public Task<Conversation?> GetUserConversationWithMessagesAsync(
            Guid userId,
            Guid conversationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Conversations.SingleOrDefault(
                conversation => conversation.Id == conversationId && conversation.UserId == userId));

        public Task<Conversation?> GetUserConversationAsync(
            Guid userId,
            Guid conversationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Conversations.SingleOrDefault(
                conversation => conversation.Id == conversationId && conversation.UserId == userId));

        public Task AddMessageAsync(Message message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public void DeleteConversation(Conversation conversation) =>
            Conversations.Remove(conversation);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }
}
