namespace TaskAndDocumentManager.Domain.Workspaces;

public class WorkspaceMember
{
    public const int MaxRoleLength = 100;

    public Guid WorkspaceId { get; private set; }

    public Guid UserId { get; private set; }

    public string Role { get; private set; } = string.Empty;

    public DateTime JoinedAtUtc { get; private set; } = DateTime.UtcNow;

    protected WorkspaceMember()
    {
    }

    public WorkspaceMember(Guid workspaceId, Guid userId, string role)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            throw new ArgumentException("Workspace role is required.", nameof(role));
        }

        var normalizedRole = WorkspaceRoles.Normalize(role);
        if (normalizedRole.Length > MaxRoleLength)
        {
            throw new ArgumentException($"Workspace role cannot exceed {MaxRoleLength} characters.", nameof(role));
        }

        WorkspaceId = workspaceId;
        UserId = userId;
        Role = normalizedRole;
    }
}
