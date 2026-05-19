using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PrivateAiChat.Contracts.Auth;

namespace PrivateAiChat.Web.Services;

public sealed class AuthApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ApiCookieStore _cookieStore;

    public AuthApiClient(IOptions<ApiClientOptions> options, ApiCookieStore cookieStore)
    {
        _cookieStore = cookieStore;
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
        try
        {
            using var response = await _httpClient.PostAsync("auth/logout", content: null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await _cookieStore.PersistAsync();
            }

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

    private async Task<ApiResult<TResponse>> PostAsync<TResponse>(
        string path,
        object request,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(path, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<TResponse>.Failure(await ApiErrorParser.ReadErrorAsync(
                    response,
                    cancellationToken,
                    unauthorizedMessage: "The email or password is incorrect."));
            }

            var value = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
            await _cookieStore.PersistAsync();
            return value is null
                ? ApiResult<TResponse>.Failure("The API returned an empty response.")
                : ApiResult<TResponse>.Success(value);
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
