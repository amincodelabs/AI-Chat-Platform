using System.Net.Http.Headers;

namespace PrivateAiChat.Web.Services;

public sealed class ApiCookieHandler : DelegatingHandler
{
    private readonly ApiCookieStore _cookieStore;

    public ApiCookieHandler(ApiCookieStore cookieStore)
    {
        _cookieStore = cookieStore;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _cookieStore.Apply(request);

        var response = await base.SendAsync(request, cancellationToken);
        _cookieStore.UpdateFromResponse(response);
        return response;
    }
}
