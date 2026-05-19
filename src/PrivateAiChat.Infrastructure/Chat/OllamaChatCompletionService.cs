using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PrivateAiChat.Application.Chat;

namespace PrivateAiChat.Infrastructure.Chat;

public sealed class OllamaChatCompletionService : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public OllamaChatCompletionService(HttpClient httpClient, IOptions<OllamaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> CompleteAsync(
        IReadOnlyCollection<ChatCompletionMessage> messages,
        CancellationToken cancellationToken)
    {
        var request = new OllamaChatRequest(
            _options.Model,
            Stream: false,
            messages.Select(message => new OllamaMessage(message.Role, message.Content)).ToArray());

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new ChatCompletionException("Ollama is unavailable.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ChatCompletionException("Ollama request timed out.", exception);
        }

        using var _ = response;

        if (!response.IsSuccessStatusCode)
        {
            throw new ChatCompletionException($"Ollama returned HTTP {(int)response.StatusCode}.");
        }

        var completion = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken);
        var content = completion?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ChatCompletionException("Ollama returned an empty assistant response.");
        }

        return content;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        IReadOnlyCollection<ChatCompletionMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new OllamaChatRequest(
            _options.Model,
            Stream: true,
            messages.Select(message => new OllamaMessage(message.Role, message.Content)).ToArray());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/chat")
        {
            Content = JsonContent.Create(request)
        };

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new ChatCompletionException("Ollama is unavailable.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ChatCompletionException("Ollama request timed out.", exception);
        }

        using var _ = response;

        if (!response.IsSuccessStatusCode)
        {
            throw new ChatCompletionException($"Ollama returned HTTP {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            OllamaChatResponse? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaChatResponse>(
                    line,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch (JsonException exception)
            {
                throw new ChatCompletionException("Ollama returned an invalid streaming response.", exception);
            }

            var content = chunk?.Message?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
            }
        }
    }

    private sealed record OllamaChatRequest(
        string Model,
        bool Stream,
        IReadOnlyCollection<OllamaMessage> Messages);

    private sealed record OllamaMessage(
        string Role,
        string Content);

    private sealed record OllamaChatResponse(
        OllamaMessage? Message);
}
