namespace PrivateAiChat.Application.Chat;

public sealed class ChatCompletionException : Exception
{
    public ChatCompletionException(string message, string code = "chat_completion_failed")
        : base(message)
    {
        Code = code;
    }

    public ChatCompletionException(
        string message,
        Exception innerException,
        string code = "chat_completion_failed")
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}
