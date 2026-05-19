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
            response = await SendWithRetryAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new ChatCompletionException("Ollama is unavailable.", exception, "ollama_unavailable");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ChatCompletionException("Ollama request timed out.", exception, "ollama_timeout");
        }

        using var _ = response;

        if (!response.IsSuccessStatusCode)
        {
            throw new ChatCompletionException($"Ollama returned HTTP {(int)response.StatusCode}.", "ollama_http_error");
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
            throw new ChatCompletionException("Ollama is unavailable.", exception, "ollama_unavailable");
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ChatCompletionException("Ollama request timed out.", exception, "ollama_timeout");
        }

        using var _ = response;

        if (!response.IsSuccessStatusCode)
        {
            throw new ChatCompletionException($"Ollama returned HTTP {(int)response.StatusCode}.", "ollama_http_error");
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
                throw new ChatCompletionException(
                    "Ollama returned an invalid streaming response.",
                    exception,
                    "ollama_invalid_response");
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

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        OllamaChatRequest request,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 2;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                continue;
            }

            if (!ShouldRetry(response) || attempt == maxAttempts)
            {
                return response;
            }

            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        throw new InvalidOperationException("Retry loop exited unexpectedly.");
    }

    private static bool ShouldRetry(HttpResponseMessage response) =>
        response.StatusCode is
            System.Net.HttpStatusCode.RequestTimeout or
            System.Net.HttpStatusCode.TooManyRequests or
            System.Net.HttpStatusCode.BadGateway or
            System.Net.HttpStatusCode.ServiceUnavailable or
            System.Net.HttpStatusCode.GatewayTimeout;

    private sealed record OllamaMessage(
        string Role,
        string Content);

    private sealed record OllamaChatResponse(
        OllamaMessage? Message);
}
