namespace TaskAndDocumentManager.Domain.Workspaces;

public class Workspace
{
    public const int MaxNameLength = 200;

    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Name { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public Guid CreatedByUserId { get; private set; }

    protected Workspace()
    {
    }

    public Workspace(string name, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workspace name is required.", nameof(name));
        }

        if (name.Trim().Length > MaxNameLength)
        {
            throw new ArgumentException($"Workspace name cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Created by user ID is required.", nameof(createdByUserId));
        }

        Name = name.Trim();
        CreatedByUserId = createdByUserId;
    }
}
