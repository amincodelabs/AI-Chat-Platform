using Microsoft.Extensions.DependencyInjection;
using PrivateAiChat.Application.Conversations;

namespace PrivateAiChat.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IConversationService, ConversationService>();

        return services;
    }
}
