using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PrivateAiChat.Contracts.Conversations;

namespace PrivateAiChat.Web.Services;

public sealed class ConversationApiClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public ConversationApiClient(IOptions<ApiClientOptions> options, ApiCookieStore cookieStore)
    {
        var apiBaseUrl = new Uri(options.Value.BaseUrl, UriKind.Absolute);
        var handler = new ApiCookieHandler(cookieStore)
        {
            InnerHandler = new HttpClientHandler
            {
                UseCookies = false
            }
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = apiBaseUrl
        };
    }

    public Task<ApiResult<IReadOnlyCollection<ConversationSummaryResponse>>> GetConversationsAsync(
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            using var response = await _httpClient.GetAsync("api/conversations", cancellationToken);
            return await ReadResponseAsync<IReadOnlyCollection<ConversationSummaryResponse>>(response, cancellationToken);
        }, cancellationToken);

    public Task<ApiResult<ConversationDetailsResponse>> GetConversationAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            using var response = await _httpClient.GetAsync($"api/conversations/{id}", cancellationToken);
            return await ReadResponseAsync<ConversationDetailsResponse>(response, cancellationToken);
        }, cancellationToken);

    public Task<ApiResult<ConversationSummaryResponse>> CreateConversationAsync(
        CreateConversationRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            using var response = await _httpClient.PostAsJsonAsync("api/conversations", request, cancellationToken);
            return await ReadResponseAsync<ConversationSummaryResponse>(response, cancellationToken);
        }, cancellationToken);

    public Task<ApiResult<AddMessageResponse>> AddMessageAsync(
        Guid conversationId,
        AddMessageRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async () =>
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"api/conversations/{conversationId}/messages",
                request,
                cancellationToken);

            return await ReadResponseAsync<AddMessageResponse>(response, cancellationToken);
        }, cancellationToken);

    public async Task<ApiResult> AddMessageStreamingAsync(
        Guid conversationId,
        AddMessageRequest request,
        Func<MessageResponse, Task> onUserMessage,
        Func<string, Task> onAssistantChunk,
        Func<MessageResponse, Task> onAssistantMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/conversations/{conversationId}/messages/stream")
            {
                Content = JsonContent.Create(request)
            };

            using var response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult.Failure(await ApiErrorParser.ReadErrorAsync(response, cancellationToken));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
                {
                    continue;
                }

                ChatStreamEvent? streamEvent;
                try
                {
                    streamEvent = JsonSerializer.Deserialize<ChatStreamEvent>(
                        line["data: ".Length..],
                        new JsonSerializerOptions(JsonSerializerDefaults.Web));
                }
                catch (JsonException)
                {
                    return ApiResult.Failure("The API returned an invalid streaming response.");
                }

                if (streamEvent is null)
                {
                    continue;
                }

                switch (streamEvent.Type)
                {
                    case ChatStreamEvent.UserMessage when streamEvent.Message is not null:
                        await onUserMessage(streamEvent.Message);
                        break;
                    case ChatStreamEvent.AssistantChunk when streamEvent.Content is not null:
                        await onAssistantChunk(streamEvent.Content);
                        break;
                    case ChatStreamEvent.AssistantMessage when streamEvent.Message is not null:
                        await onAssistantMessage(streamEvent.Message);
                        break;
                    case ChatStreamEvent.NotFound:
                        return ApiResult.Failure("Conversation was not found.");
                    case ChatStreamEvent.Error:
                        return ApiResult.Failure(streamEvent.Content ?? "The streaming response failed.");
                }
            }

            return ApiResult.Success();
        }
        catch (Exception exception) when (IsNetworkFailure(exception, cancellationToken))
        {
            return ApiResult.Failure(ToNetworkError(exception));
        }
    }

    public async Task<ApiResult> DeleteConversationAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.DeleteAsync($"api/conversations/{id}", cancellationToken);
            return response.IsSuccessStatusCode
                ? ApiResult.Success()
                : ApiResult.Failure(await ApiErrorParser.ReadErrorAsync(response, cancellationToken));
        }
        catch (Exception exception) when (IsNetworkFailure(exception, cancellationToken))
        {
            return ApiResult.Failure(ToNetworkError(exception));
        }
    }

    public void Dispose() => _httpClient.Dispose();

    private static async Task<ApiResult<TResponse>> ReadResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<TResponse>.Failure(await ApiErrorParser.ReadErrorAsync(response, cancellationToken));
        }

        var value = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        return value is null
            ? ApiResult<TResponse>.Failure("The API returned an empty response.")
            : ApiResult<TResponse>.Success(value);
    }

    private static async Task<ApiResult<TResponse>> ExecuteAsync<TResponse>(
        Func<Task<ApiResult<TResponse>>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (IsNetworkFailure(exception, cancellationToken))
        {
            return ApiResult<TResponse>.Failure(ToNetworkError(exception));
        }
    }

    private static bool IsNetworkFailure(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested &&
        exception is HttpRequestException or TaskCanceledException or IOException;

    private static string ToNetworkError(Exception exception) =>
        exception is TaskCanceledException
            ? "The request timed out. Please try again."
            : "The API could not be reached. Please check the connection and try again.";
}
