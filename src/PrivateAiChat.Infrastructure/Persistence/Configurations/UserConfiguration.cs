using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateAiChat.Domain.Users;

namespace PrivateAiChat.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(user => user.UserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(user => user.DisplayName)
            .HasMaxLength(100);

        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();
        builder.Property(user => user.DeletedAt);
        builder.Property(user => user.IsDeleted).IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasQueryFilter(user => !user.IsDeleted);
    }
}
