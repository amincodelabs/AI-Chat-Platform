using PrivateAiChat.Contracts.Auth;

namespace PrivateAiChat.Web.Services;

public sealed class AuthStateService
{
    public event Action? Changed;

    public AuthResponse? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public void SetUser(AuthResponse user)
    {
        CurrentUser = user;
        Changed?.Invoke();
    }

    public void Clear()
    {
        CurrentUser = null;
        Changed?.Invoke();
    }
}
