using Xunit;
using PrivateAiChat.Domain.Conversations;
using PrivateAiChat.Domain.Messages;
using PrivateAiChat.Domain.Users;

namespace PrivateAiChat.Domain.Tests;

public sealed class EntityTests
{
    [Fact]
    public void User_Trims_Email_And_DisplayName()
    {
        var user = new User("  test@example.com  ", "  Test User  ");

        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("Test User", user.DisplayName);
        Assert.NotEqual(Guid.Empty, user.Id);
    }

    [Fact]
    public void Conversation_Requires_UserId_And_Trims_Title()
    {
        var userId = Guid.NewGuid();
        var conversation = new Conversation(userId, "  My Chat  ");

        Assert.Equal(userId, conversation.UserId);
        Assert.Equal("My Chat", conversation.Title);
        Assert.NotEqual(Guid.Empty, conversation.Id);
    }

    [Fact]
    public void Message_Requires_ConversationId_And_Content()
    {
        var conversationId = Guid.NewGuid();
        var message = new Message(conversationId, MessageRole.Assistant, "  Hello world  ");

        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(MessageRole.Assistant, message.Role);
        Assert.Equal("Hello world", message.Content);
        Assert.NotEqual(Guid.Empty, message.Id);
    }

    [Fact]
    public void AuditableFields_Are_Initialized()
    {
        var entity = new User("user@example.com");

        Assert.True(entity.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.Equal(entity.CreatedAt, entity.UpdatedAt);
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
    }
}
