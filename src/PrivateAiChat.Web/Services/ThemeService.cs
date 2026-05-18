using Microsoft.JSInterop;

namespace PrivateAiChat.Web.Services;

public sealed class ThemeService
{
    private const string StorageKey = "private-ai-chat-theme";
    private readonly IJSRuntime _jsRuntime;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string CurrentTheme { get; private set; } = "dark";

    public async Task InitializeAsync()
    {
        var storedTheme = await _jsRuntime.InvokeAsync<string?>("privateAiChatTheme.get", StorageKey);
        CurrentTheme = storedTheme is "light" or "dark" ? storedTheme : "dark";
        await ApplyAsync(CurrentTheme);
    }

    public async Task ToggleAsync()
    {
        CurrentTheme = CurrentTheme == "dark" ? "light" : "dark";
        await ApplyAsync(CurrentTheme);
    }

    private async Task ApplyAsync(string theme) =>
        await _jsRuntime.InvokeVoidAsync("privateAiChatTheme.set", StorageKey, theme);
}
