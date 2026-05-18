using System.Net;

namespace PrivateAiChat.Web.Services;

public sealed class ApiCookieStore
{
    public CookieContainer Cookies { get; } = new();
}
