using System.ComponentModel.DataAnnotations;
using PrivateAiChat.Contracts.Auth;
using Xunit;

namespace PrivateAiChat.Contracts.Tests;

public sealed class AuthRequestValidationTests
{
    [Fact]
    public void SignupRequest_Requires_Valid_Email()
    {
        var request = new SignupRequest("not-an-email", "Password1", null);

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(SignupRequest.Email)));
    }

    [Fact]
    public void SignupRequest_Requires_Reasonable_Password_Length()
    {
        var request = new SignupRequest("user@example.com", "short", null);

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(SignupRequest.Password)));
    }

    [Fact]
    public void LoginRequest_Accepts_Valid_Credentials_Shape()
    {
        var request = new LoginRequest("user@example.com", "Password1");

        var results = Validate(request);

        Assert.Empty(results);
    }

    private static List<ValidationResult> Validate<TRequest>(TRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            request!,
            new ValidationContext(request!),
            results,
            validateAllProperties: true);

        return results;
    }
}
