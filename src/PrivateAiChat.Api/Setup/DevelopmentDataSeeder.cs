using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PrivateAiChat.Domain.Conversations;
using PrivateAiChat.Domain.Messages;
using PrivateAiChat.Domain.Users;
using PrivateAiChat.Infrastructure.Persistence;

namespace PrivateAiChat.Api.Setup;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DevelopmentDataSeeder");

        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("DevelopmentSeed:Enabled"))
        {
            return;
        }

        var email = configuration["DevelopmentSeed:TestUser:Email"]!.Trim();
        var password = configuration["DevelopmentSeed:TestUser:Password"]!;
        var displayName = configuration["DevelopmentSeed:TestUser:DisplayName"]?.Trim();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User(email, displayName);
            user.EmailConfirmed = true;

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Development seed user could not be created: {errors}");
            }

            logger.LogInformation("Created development seed user {Email}.", email);
        }

        var hasSeedConversations = await dbContext.Conversations
            .AnyAsync(conversation => conversation.UserId == user.Id, cancellationToken);

        if (hasSeedConversations)
        {
            return;
        }

        var onboarding = new Conversation(user.Id, "Getting started with PrivateAiChat");
        var planning = new Conversation(user.Id, "Local development checklist");

        dbContext.Conversations.AddRange(onboarding, planning);

        dbContext.Messages.AddRange(
            new Message(onboarding.Id, MessageRole.User, "What can I use this private chat workspace for?"),
            new Message(onboarding.Id, MessageRole.Assistant, "Use it for local AI conversations where your app owns the chat history and authentication flow."),
            new Message(planning.Id, MessageRole.User, "What should I check before running the Docker stack?"),
            new Message(planning.Id, MessageRole.Assistant, "Confirm SQL Server, Redis, and Ollama settings, then apply migrations before testing chat."));

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Created development seed conversations for {Email}.", email);
    }
}
