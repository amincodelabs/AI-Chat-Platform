using Microsoft.AspNetCore.Identity;
using PrivateAiChat.Api.Common.Startup;
using PrivateAiChat.Api.RateLimiting;
using PrivateAiChat.Contracts.Auth;
using PrivateAiChat.Domain.Users;

namespace PrivateAiChat.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/signup", async (
            SignupRequest request,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            HttpContext httpContext) =>
        {
            if (!ValidationHelpers.TryValidate(request, out var validationErrors))
            {
                return ErrorResponseHelpers.ValidationError(httpContext, validationErrors);
            }

            var existingUser = await userManager.FindByEmailAsync(request.Email.Trim());
            if (existingUser is not null)
            {
                return ErrorResponseHelpers.ValidationError(httpContext, new Dictionary<string, string[]>
                {
                    [nameof(SignupRequest.Email)] = ["Signup could not be completed."]
                });
            }

            var user = new User(request.Email, request.DisplayName);
            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                return ErrorResponseHelpers.ValidationError(httpContext, ValidationHelpers.ToValidationErrors(result));
            }

            await signInManager.SignInAsync(user, isPersistent: true);

            return Results.Ok(AuthHelpers.ToAuthResponse(user));
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicyNames.Auth);

        app.MapPost("/auth/login", async (
            LoginRequest request,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            HttpContext httpContext) =>
        {
            if (!ValidationHelpers.TryValidate(request, out var validationErrors))
            {
                return ErrorResponseHelpers.ValidationError(httpContext, validationErrors);
            }

            var user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null)
            {
                return ErrorResponseHelpers.UnauthorizedError(httpContext);
            }

            var result = await signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: true);

            return result.Succeeded
                ? Results.Ok(AuthHelpers.ToAuthResponse(user))
                : ErrorResponseHelpers.UnauthorizedError(httpContext);
        })
        .AllowAnonymous()
        .RequireRateLimiting(RateLimitPolicyNames.Auth);

        app.MapPost("/auth/logout", async (SignInManager<User> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        })
        .RequireAuthorization()
        .RequireRateLimiting(RateLimitPolicyNames.Auth);

        return app;
    }
}
