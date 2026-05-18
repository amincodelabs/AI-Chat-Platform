namespace PrivateAiChat.Web.Services;

public sealed class ApiClientOptions
{
    public const string SectionName = "Api";

    public string BaseUrl { get; set; } = "https://localhost:7000";
}
