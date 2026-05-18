namespace TaskAndDocumentManager.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTime TimestampUtc { get; private set; } = DateTime.UtcNow;

    protected AuditLog()
    {
    }

    public AuditLog(
        Guid userId,
        string action,
        string entityType,
        Guid entityId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.", nameof(action));
        }

        if (!AuditActions.IsValid(action))
        {
            throw new ArgumentException("Action must be a supported audit action.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type is required.", nameof(entityType));
        }

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("Entity ID is required.", nameof(entityId));
        }

        UserId = userId;
        Action = action.Trim();
        EntityType = entityType.Trim();
        EntityId = entityId;
    }
}
