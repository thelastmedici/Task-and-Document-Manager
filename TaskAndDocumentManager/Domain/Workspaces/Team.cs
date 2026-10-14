namespace TaskAndDocumentManager.Domain.Workspaces;

public class Team
{
    public const int MaxNameLength = 200;

    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;


    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

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
    public void UpdateName(string newName)
    {
        ValidateName(newName);
        Name = newName.Trim();
        UpdateTimestamp();
    }

    public void UpdateTimestamp() 
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

 
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

