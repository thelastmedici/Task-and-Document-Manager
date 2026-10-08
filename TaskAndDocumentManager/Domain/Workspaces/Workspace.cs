namespace TaskAndDocumentManager.Domain.Workspaces;

public class Workspace
{
    public const int MaxNameLength = 200;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; private set; }

    protected Workspace() { }

    public Workspace(string name, Guid createdByUserId)
    {
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("Created by user ID is required.", nameof(createdByUserId));

        Name = NormalizeAndValidateName(name);
        CreatedByUserId = createdByUserId;
    }

    public void UpdateName(string newName)
    {
        Name = NormalizeAndValidateName(newName);
        UpdateTimestamp();
    }

    public void UpdateTimestamp() => UpdatedAtUtc = DateTime.UtcNow;

    private static string NormalizeAndValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name is required.", nameof(name));

        var trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
            throw new ArgumentException($"Workspace name cannot exceed {MaxNameLength} characters.", nameof(name));

        return trimmed;
    }
}