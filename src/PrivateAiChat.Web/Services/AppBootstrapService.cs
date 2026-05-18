namespace PrivateAiChat.Web.Services;

public sealed class AppBootstrapService
{
    private readonly ThemeService _themeService;
    private readonly ApiCookieStore _cookieStore;
    private readonly AuthStateService _authStateService;

    public AppBootstrapService(
        ThemeService themeService,
        ApiCookieStore cookieStore,
        AuthStateService authStateService)
    {
        _themeService = themeService;
        _cookieStore = cookieStore;
        _authStateService = authStateService;
    }

    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        if (IsInitialized)
        {
            return;
        }

        try
        {
            await _themeService.InitializeAsync();
            await _cookieStore.InitializeAsync();
            await _authStateService.InitializeAsync();
        }
        catch
        {
            // Fail open so a browser storage or JS issue does not block the app shell.
        }
        finally
        {
            IsInitialized = true;
        }
    }
}
