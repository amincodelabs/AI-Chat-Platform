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

    public string CurrentTheme { get; private set; } = "system";

    public async Task InitializeAsync()
    {
        try
        {
            var storedTheme = await _jsRuntime.InvokeAsync<string?>("privateAiChatTheme.get", StorageKey);
            CurrentTheme = storedTheme is "light" or "dark" or "system" ? storedTheme : "system";
            await ApplyAsync(CurrentTheme);
        }
        catch
        {
            CurrentTheme = "system";
        }
    }

    public async Task ToggleAsync()
    {
        CurrentTheme = CurrentTheme == "dark" ? "light" : "dark";
        try
        {
            await ApplyAsync(CurrentTheme);
        }
        catch
        {
        }
    }

    public async Task SetThemeAsync(string theme)
    {
        CurrentTheme = theme is "light" or "dark" or "system" ? theme : "system";
        try
        {
            await ApplyAsync(CurrentTheme);
        }
        catch
        {
        }
    }

    private async Task ApplyAsync(string theme) =>
        await _jsRuntime.InvokeVoidAsync("privateAiChatTheme.set", StorageKey, theme);
}
