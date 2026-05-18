using System.Net.Http.Json;
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
