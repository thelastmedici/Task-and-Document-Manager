namespace TaskAndDocumentManager.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    protected AuditLog()
    {
    }

    public AuditLog(
        Guid userId,
        string action,
        string entityType,
        Guid entityId,
        Guid workspaceId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
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
        WorkspaceId = workspaceId;
        Action = action.Trim();
        EntityType = entityType.Trim();
        EntityId = entityId;
        var now = DateTime.UtcNow;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        TimestampUtc = now;
    }

    public void UpdateTimestamp()
    {
        var now = DateTime.UtcNow;
        UpdatedAtUtc = now;
        TimestampUtc = now;
    }
}
