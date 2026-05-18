using System.Text.Json;
using PrivateAiChat.Contracts.Auth;
using Microsoft.JSInterop;

namespace PrivateAiChat.Web.Services;

public sealed class AuthStateService
{
    private const string StorageKey = "private-ai-chat-auth-user";

    private readonly IJSRuntime _jsRuntime;

    public AuthStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public event Action? Changed;

    public AuthResponse? CurrentUser { get; private set; }

    public bool IsAuthenticated => IsInitialized && CurrentUser is not null;

    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        try
        {
            var payload = await _jsRuntime.InvokeAsync<string?>("privateAiChatState.get", StorageKey);
            if (!string.IsNullOrWhiteSpace(payload))
            {
                try
                {
                    CurrentUser = JsonSerializer.Deserialize<AuthResponse>(payload);
                }
                catch (JsonException)
                {
                    CurrentUser = null;
                }
            }
        }
        catch
        {
            CurrentUser = null;
        }

        IsInitialized = true;
        Changed?.Invoke();
    }

    public async Task SetUserAsync(AuthResponse user)
    {
        CurrentUser = user;
        IsInitialized = true;
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "privateAiChatState.set",
                StorageKey,
                JsonSerializer.Serialize(user));
        }
        catch
        {
        }
        Changed?.Invoke();
    }

    public async Task ClearAsync()
    {
        CurrentUser = null;
        IsInitialized = true;
        try
        {
            await _jsRuntime.InvokeVoidAsync("privateAiChatState.remove", StorageKey);
        }
        catch
        {
        }
        Changed?.Invoke();
    }
}
