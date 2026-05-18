namespace PrivateAiChat.Application.Chat;

public interface IChatCompletionService
{
    Task<string> CompleteAsync(
        IReadOnlyCollection<ChatCompletionMessage> messages,
        CancellationToken cancellationToken);
}
