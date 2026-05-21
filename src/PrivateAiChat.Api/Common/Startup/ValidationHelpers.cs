using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PrivateAiChat.Api.Common.Startup;

public static class ValidationHelpers
{
    public static bool TryValidate<TRequest>(
        TRequest request,
        out Dictionary<string, string[]> validationErrors)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(request!);

        var isValid = Validator.TryValidateObject(request!, context, results, validateAllProperties: true);
        validationErrors = results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(memberName => new { memberName, result.ErrorMessage }))
            .GroupBy(error => error.memberName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.ErrorMessage ?? "The request is invalid.")
                    .ToArray());

        return isValid;
    }

    public static Dictionary<string, string[]> ToValidationErrors(IdentityResult result) =>
        result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());
}
