using Microsoft.JSInterop;

namespace PrivateAiChat.Web.Services;

public sealed class ApiCookieStore
{
    private const string StorageKey = "private-ai-chat-auth-cookie";
    private const string CookieName = "PrivateAiChat.Auth";

    private readonly IJSRuntime _jsRuntime;

    private string? _cookieHeader;

    public ApiCookieStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsInitialized { get; private set; }

    public string? CookieHeader => _cookieHeader;

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        _cookieHeader = await _jsRuntime.InvokeAsync<string?>("privateAiChatState.get", StorageKey);
        IsInitialized = true;
    }

    public void Apply(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(_cookieHeader))
        {
            return;
        }

        request.Headers.Remove("Cookie");
        request.Headers.TryAddWithoutValidation("Cookie", _cookieHeader);
    }

    public void UpdateFromResponse(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return;
        }

        foreach (var value in values)
        {
            if (!value.StartsWith(CookieName + "=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var cookieValue = value.Split(';', 2)[0];
            _cookieHeader = cookieValue.Equals($"{CookieName}=", StringComparison.OrdinalIgnoreCase)
                ? null
                : cookieValue;
            return;
        }
    }

    public async Task PersistAsync()
    {
        if (!IsInitialized)
        {
            await InitializeAsync();
        }

        if (string.IsNullOrWhiteSpace(_cookieHeader))
        {
            await _jsRuntime.InvokeVoidAsync("privateAiChatState.remove", StorageKey);
            return;
        }

        await _jsRuntime.InvokeVoidAsync("privateAiChatState.set", StorageKey, _cookieHeader);
    }

    public async Task ClearAsync()
    {
        _cookieHeader = null;
        IsInitialized = true;
        await _jsRuntime.InvokeVoidAsync("privateAiChatState.remove", StorageKey);
    }
}
