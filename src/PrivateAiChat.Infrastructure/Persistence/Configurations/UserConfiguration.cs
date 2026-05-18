using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateAiChat.Domain.Users;

namespace PrivateAiChat.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : AuditableEntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);

        builder.ToTable("Users");

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(user => user.DisplayName)
            .HasMaxLength(100);

        builder.HasIndex(user => user.Email)
            .IsUnique();
    }
}
