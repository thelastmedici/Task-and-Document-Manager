namespace TaskAndDocumentManager.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid? AssignedUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsComplete { get; private set; }
}
