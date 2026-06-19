namespace TaskAndDocumentManager.Domain.Workspaces;

public class Team
{
    public const int MaxNameLength = 200;

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    protected Team()
    {
    }

    public Team(Guid workspaceId, string name)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Team name is required.", nameof(name));
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException($"Team name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        WorkspaceId = workspaceId;
        Name = normalizedName;
    }
}
