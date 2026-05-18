using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateAiChat.Domain.Conversations;

namespace PrivateAiChat.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : AuditableEntityConfiguration<Conversation>
{
    public override void Configure(EntityTypeBuilder<Conversation> builder)
    {
        base.Configure(builder);

        builder.ToTable("Conversations");

        builder.Property(conversation => conversation.Title)
            .HasMaxLength(200);

        builder.HasOne<PrivateAiChat.Domain.Users.User>()
            .WithMany(user => user.Conversations)
            .HasForeignKey(conversation => conversation.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(conversation => new { conversation.UserId, conversation.CreatedAt });
    }
}
