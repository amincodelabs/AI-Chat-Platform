namespace PrivateAiChat.Application.Chat;

public sealed class ChatCompletionException : Exception
{
    public ChatCompletionException(string message) : base(message)
    {
    }

    public ChatCompletionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
