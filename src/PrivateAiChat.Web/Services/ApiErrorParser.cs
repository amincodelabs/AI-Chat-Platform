using System.Net;
using System.Text.Json;

namespace PrivateAiChat.Web.Services;

public static class ApiErrorParser
{
    public static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken,
        string? unauthorizedMessage = null)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(unauthorizedMessage))
        {
            return unauthorizedMessage;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(content))
        {
            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Authentication is required.",
                HttpStatusCode.Forbidden => "You do not have access to this resource.",
                HttpStatusCode.NotFound => "The requested resource was not found.",
                _ => $"The API returned {(int)response.StatusCode}."
            };
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;

            var message = TryGetString(root, "message")
                ?? TryGetString(root, "detail")
                ?? TryGetString(root, "title")
                ?? "The request could not be completed.";

            var validationMessages = ReadValidationMessages(root);
            if (validationMessages.Length > 0)
            {
                message = string.Join(" ", validationMessages);
            }

            var requestId = TryGetString(root, "requestId");
            return string.IsNullOrWhiteSpace(requestId)
                ? message
                : $"{message} Request ID: {requestId}";
        }
        catch (JsonException)
        {
            return content;
        }
    }

    private static string? TryGetString(JsonElement root, string propertyName) =>
        root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static string[] ReadValidationMessages(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return errors.EnumerateObject()
            .SelectMany(error => error.Value.ValueKind == JsonValueKind.Array
                ? error.Value.EnumerateArray()
                : [])
            .Select(error => error.GetString())
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Select(error => error!)
            .ToArray();
    }
}
