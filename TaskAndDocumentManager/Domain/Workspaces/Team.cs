namespace TaskAndDocumentManager.Domain.Workspaces;

public class Team
{
    public const int MaxNameLength = 200;

    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    // 💡 ADDED: Timestamps required by Step 2
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    protected Team()
    {
    }

    public Team(Guid workspaceId, string name)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        ValidateName(name);

        WorkspaceId = workspaceId;
        Name = name.Trim();
    }

    // 💡 ADDED: Allows safe updates to the team name and updates the timestamp
    public void UpdateName(string newName)
    {
        ValidateName(newName);
        Name = newName.Trim();
        UpdateTimestamp();
    }

    // 💡 ADDED: Standard way to bump the updated timestamp
    public void UpdateTimestamp() 
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    // Helper validation method to keep the code DRY
    private void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Team name is required.", nameof(name));
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException($"Team name cannot exceed {MaxNameLength} characters.", nameof(name));
        }
    }
}

