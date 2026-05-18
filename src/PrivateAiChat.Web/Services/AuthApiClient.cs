using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PrivateAiChat.Contracts.Auth;

namespace PrivateAiChat.Web.Services;

public sealed class AuthApiClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(IOptions<ApiClientOptions> options, ApiCookieStore cookieStore)
    {
        var apiBaseUrl = new Uri(options.Value.BaseUrl, UriKind.Absolute);
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieStore.Cookies,
            UseCookies = true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = apiBaseUrl
        };
    }

    public Task<ApiResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<AuthResponse>("auth/login", request, cancellationToken);

    public Task<ApiResult<AuthResponse>> SignupAsync(
        SignupRequest request,
        CancellationToken cancellationToken) =>
        PostAsync<AuthResponse>("auth/signup", request, cancellationToken);

    public async Task<ApiResult> LogoutAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsync("auth/logout", content: null, cancellationToken);
        return response.IsSuccessStatusCode
            ? ApiResult.Success()
            : ApiResult.Failure(await ReadErrorAsync(response, cancellationToken));
    }

    public void Dispose() => _httpClient.Dispose();

    private async Task<ApiResult<TResponse>> PostAsync<TResponse>(
        string path,
        object request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(path, request, cancellationToken);

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
            return "The email or password is incorrect.";
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
