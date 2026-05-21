using System.Runtime.CompilerServices;
using PrivateAiChat.Application.Chat;
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
        var service = new ConversationService(repository, new FakeChatCompletionService());
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
        var service = new ConversationService(repository, new FakeChatCompletionService());

        var result = await service.GetConversationDetailsAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RenameConversationAsync_Renames_UserOwned_Conversation()
    {
        var repository = new FakeConversationRepository();
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Original");
        repository.Conversations.Add(conversation);
        var service = new ConversationService(repository, new FakeChatCompletionService());

        var result = await service.RenameConversationAsync(
            userId,
            conversation.Id,
            new RenameConversationRequest(" Renamed chat "),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Renamed chat", result.Title);
        Assert.Equal("Renamed chat", conversation.Title);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task RenameConversationAsync_Returns_Null_When_Conversation_Is_Not_UserOwned()
    {
        var repository = new FakeConversationRepository();
        var ownerId = Guid.NewGuid();
        var conversation = new Conversation(ownerId, "Original");
        repository.Conversations.Add(conversation);
        var service = new ConversationService(repository, new FakeChatCompletionService());

        var result = await service.RenameConversationAsync(
            Guid.NewGuid(),
            conversation.Id,
            new RenameConversationRequest("Renamed chat"),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal("Original", conversation.Title);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task AddMessageAsync_Adds_User_And_Assistant_Messages_When_Conversation_Is_UserOwned()
    {
        var repository = new FakeConversationRepository();
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Owned");
        repository.Conversations.Add(conversation);
        var chatCompletion = new FakeChatCompletionService("Assistant reply");
        var service = new ConversationService(repository, chatCompletion);

        var result = await service.AddMessageAsync(
            userId,
            conversation.Id,
            new AddMessageRequest(" Hello "),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(MessageRole.User.ToString(), result.UserMessage.Role);
        Assert.Equal("Hello", result.UserMessage.Content);
        Assert.Equal(MessageRole.Assistant.ToString(), result.AssistantMessage.Role);
        Assert.Equal("Assistant reply", result.AssistantMessage.Content);
        Assert.Equal(2, repository.Messages.Count);
        Assert.All(repository.Messages, message => Assert.Equal(conversation.Id, message.ConversationId));
        Assert.Equal(2, repository.SaveChangesCount);
        Assert.Equal("user", chatCompletion.Messages.Single().Role);
        Assert.Equal("Hello", chatCompletion.Messages.Single().Content);
    }

    [Fact]
    public async Task AddMessageAsync_Generates_Title_From_First_User_Message()
    {
        var repository = new FakeConversationRepository();
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId);
        repository.Conversations.Add(conversation);
        var service = new ConversationService(repository, new FakeChatCompletionService());

        var result = await service.AddMessageAsync(
            userId,
            conversation.Id,
            new AddMessageRequest("One two three four five six seven eight nine ten"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("One two three four five six seven eight", conversation.Title);
    }

    [Fact]
    public async Task AddMessageStreamingAsync_Streams_And_Saves_Assistant_Message_When_Conversation_Is_UserOwned()
    {
        var repository = new FakeConversationRepository();
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Owned");
        repository.Conversations.Add(conversation);
        var chatCompletion = new FakeChatCompletionService("Assistant reply");
        var service = new ConversationService(repository, chatCompletion);
        var events = new List<ChatStreamEvent>();

        await foreach (var streamEvent in service.AddMessageStreamingAsync(
            userId,
            conversation.Id,
            new AddMessageRequest("Hello"),
            CancellationToken.None))
        {
            events.Add(streamEvent);
        }

        Assert.Collection(
            events,
            streamEvent => Assert.Equal(ChatStreamEvent.UserMessage, streamEvent.Type),
            streamEvent =>
            {
                Assert.Equal(ChatStreamEvent.AssistantChunk, streamEvent.Type);
                Assert.Equal("Assistant reply", streamEvent.Content);
            },
            streamEvent => Assert.Equal(ChatStreamEvent.AssistantMessage, streamEvent.Type));
        Assert.Equal(2, repository.Messages.Count);
        Assert.Equal(MessageRole.Assistant, repository.Messages.Last().Role);
        Assert.Equal("Assistant reply", repository.Messages.Last().Content);
        Assert.Equal(2, repository.SaveChangesCount);
    }

    [Fact]
    public async Task AddMessageStreamingAsync_Saves_Partial_Assistant_Message_When_Cancelled()
    {
        var repository = new FakeConversationRepository();
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "Owned");
        repository.Conversations.Add(conversation);
        var chatCompletion = new FakeChatCompletionService("Partial", " reply");
        var service = new ConversationService(repository, chatCompletion);
        using var cancellationTokenSource = new CancellationTokenSource();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var streamEvent in service.AddMessageStreamingAsync(
                userId,
                conversation.Id,
                new AddMessageRequest("Hello"),
                cancellationTokenSource.Token))
            {
                if (streamEvent.Type == ChatStreamEvent.AssistantChunk)
                {
                    await cancellationTokenSource.CancelAsync();
                }
            }
        });

        Assert.Equal(2, repository.Messages.Count);
        Assert.Equal(MessageRole.Assistant, repository.Messages.Last().Role);
        Assert.Equal("Partial", repository.Messages.Last().Content);
        Assert.Equal(2, repository.SaveChangesCount);
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

    private sealed class FakeChatCompletionService : IChatCompletionService
    {
        private readonly string[] _responses;

        public FakeChatCompletionService(params string[] responses)
        {
            _responses = responses.Length == 0 ? ["Test response"] : responses;
        }

        public IReadOnlyCollection<ChatCompletionMessage> Messages { get; private set; } = [];

        public Task<string> CompleteAsync(
            IReadOnlyCollection<ChatCompletionMessage> messages,
            CancellationToken cancellationToken)
        {
            Messages = messages;
            return Task.FromResult(string.Concat(_responses));
        }

        public async IAsyncEnumerable<string> StreamAsync(
            IReadOnlyCollection<ChatCompletionMessage> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Messages = messages;
            foreach (var response in _responses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return response;
            }
        }
    }
}
