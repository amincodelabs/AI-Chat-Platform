using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateAiChat.Domain.Messages;

namespace PrivateAiChat.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : AuditableEntityConfiguration<Message>
{
    public override void Configure(EntityTypeBuilder<Message> builder)
    {
        base.Configure(builder);

        builder.ToTable("Messages");

        builder.Property(message => message.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(message => message.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne<PrivateAiChat.Domain.Conversations.Conversation>()
            .WithMany(conversation => conversation.Messages)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(message => new { message.ConversationId, message.CreatedAt });
    }
}
