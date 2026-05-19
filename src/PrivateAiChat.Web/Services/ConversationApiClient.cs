using System.Net;
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

    public async Task<ApiResult<IReadOnlyCollection<ConversationSummaryResponse>>> GetConversationsAsync(
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("api/conversations", cancellationToken);
        return await ReadResponseAsync<IReadOnlyCollection<ConversationSummaryResponse>>(response, cancellationToken);
    }

    public async Task<ApiResult<ConversationDetailsResponse>> GetConversationAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"api/conversations/{id}", cancellationToken);
        return await ReadResponseAsync<ConversationDetailsResponse>(response, cancellationToken);
    }

    public async Task<ApiResult<ConversationSummaryResponse>> CreateConversationAsync(
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/conversations", request, cancellationToken);
        return await ReadResponseAsync<ConversationSummaryResponse>(response, cancellationToken);
    }

    public async Task<ApiResult<AddMessageResponse>> AddMessageAsync(
        Guid conversationId,
        AddMessageRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            $"api/conversations/{conversationId}/messages",
            request,
            cancellationToken);

        return await ReadResponseAsync<AddMessageResponse>(response, cancellationToken);
    }

    public async Task<ApiResult> AddMessageStreamingAsync(
        Guid conversationId,
        AddMessageRequest request,
        Func<MessageResponse, Task> onUserMessage,
        Func<string, Task> onAssistantChunk,
        Func<MessageResponse, Task> onAssistantMessage,
        CancellationToken cancellationToken)
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
            return ApiResult.Failure(await ReadErrorAsync(response, cancellationToken));
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

    public async Task<ApiResult> DeleteConversationAsync(Guid id, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync($"api/conversations/{id}", cancellationToken);
        return response.IsSuccessStatusCode
            ? ApiResult.Success()
            : ApiResult.Failure(await ReadErrorAsync(response, cancellationToken));
    }

    public void Dispose() => _httpClient.Dispose();

    private static async Task<ApiResult<TResponse>> ReadResponseAsync<TResponse>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return ApiResult<TResponse>.Failure(await ReadErrorAsync(response, cancellationToken));
        }

        var value = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        return value is null
            ? ApiResult<TResponse>.Failure("The API returned an empty response.")
            : ApiResult<TResponse>.Success(value);
    }

    private static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            return "Authentication is required.";
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"The API returned {(int)response.StatusCode}.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                var messages = errors.EnumerateObject()
                    .SelectMany(error => error.Value.EnumerateArray())
                    .Select(error => error.GetString())
                    .Where(error => !string.IsNullOrWhiteSpace(error))
                    .ToArray();

                if (messages.Length > 0)
                {
                    return string.Join(" ", messages);
                }
            }

            if (document.RootElement.TryGetProperty("detail", out var detail))
            {
                return detail.GetString() ?? "The request could not be completed.";
            }

            if (document.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? "The request could not be completed.";
            }
        }
        catch (JsonException)
        {
            return content;
        }

        return "The request could not be completed.";
    }
}
