namespace PrivateAiChat.Domain.Common;

public abstract class AuditableEntity
{
    protected AuditableEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DeletedAt.Value;
    }
}
