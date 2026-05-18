using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PrivateAiChat.Domain.Common;

namespace PrivateAiChat.Infrastructure.Persistence.Configurations;

public abstract class AuditableEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : AuditableEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.CreatedAt).IsRequired();
        builder.Property(entity => entity.UpdatedAt).IsRequired();
        builder.Property(entity => entity.DeletedAt);
        builder.Property(entity => entity.IsDeleted).IsRequired();
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
