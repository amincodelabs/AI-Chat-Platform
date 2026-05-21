using System.Text.Json;
using PrivateAiChat.Contracts.Conversations;

namespace PrivateAiChat.Api.Common.Startup;

public static class ServerSentEventHelpers
{
    public static async Task WriteServerSentEventAsync(
        HttpResponse response,
        ChatStreamEvent streamEvent,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(
            streamEvent,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        await response.WriteAsync($"event: {streamEvent.Type}\n", cancellationToken);
        await response.WriteAsync($"data: {payload}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
