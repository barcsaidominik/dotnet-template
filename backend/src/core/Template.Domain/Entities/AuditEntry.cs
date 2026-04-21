using Template.Domain.Enums;

namespace Template.Domain.Entities;

public class AuditEntry
{
    public long Id
    {
        get; private set;
    }
    public string EntityType
    {
        get; private set;
    } = default!;
    public string EntityId
    {
        get; private set;
    } = default!;
    public AuditAction Action
    {
        get; private set;
    }
    public Guid? UserId
    {
        get; private set;
    }
    public string? UserEmail
    {
        get; private set;
    }
    public string? ChangesJson
    {
        get; private set;
    }
    public DateTime OccurredAt
    {
        get; private set;
    }

    private AuditEntry()
    {
    }

    public static AuditEntry Create(
        string entityType,
        string entityId,
        AuditAction action,
        Guid? userId,
        string? userEmail,
        string? changesJson)
    {
        return new AuditEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            UserEmail = userEmail,
            ChangesJson = changesJson,
            OccurredAt = DateTime.UtcNow
        };
    }
}
