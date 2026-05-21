using System.Security.Claims;
using PrivateAiChat.Contracts.Auth;
using PrivateAiChat.Domain.Users;

namespace PrivateAiChat.Api.Common.Startup;

public static class AuthHelpers
{
    public static AuthResponse ToAuthResponse(User user) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName);

    public static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdValue, out userId);
    }
}
