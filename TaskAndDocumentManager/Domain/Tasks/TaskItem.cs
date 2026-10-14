namespace TaskAndDocumentManager.Domain.Tasks;

public class TaskItem
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 4000;

    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? AssignedToUserId { get; set; }

    public Guid OwnerId { get; set; }

    public Guid WorkspaceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DueAtUtc { get; set; }

    public DateTime? DeadlineReminderSentAtUtc { get; set; }

    public TaskPriority Priority { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    protected TaskItem() { }

    public TaskItem(
        string title,
        string description,
        Guid ownerId,
        DateTime? dueAtUtc = null,
        TaskPriority priority = TaskPriority.Medium)
        : this(title, description, ownerId, ownerId, dueAtUtc, priority)
    {
    }

    public TaskItem(
        string title,
        string description,
        Guid ownerId,
        Guid workspaceId,
        DateTime? dueAtUtc = null,
        TaskPriority priority = TaskPriority.Medium)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner ID is required.", nameof(ownerId));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        Id = Guid.NewGuid();
        OwnerId = ownerId;
        WorkspaceId = workspaceId;
        CreatedAt = DateTime.UtcNow;
        Title = NormalizeRequiredText(title, nameof(title), MaxTitleLength);
        Description = NormalizeRequiredText(description, nameof(description), MaxDescriptionLength);
        DueAtUtc = dueAtUtc;
        Priority = ValidatePriority(priority);
        IsCompleted = false;
    }

    public void AssignTask(Guid? userId)
    {
        EnsureTaskIsNotCompleted();

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (AssignedToUserId == userId)
        {
            return;
        }

        AssignedToUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnassignTask()
    {
        AssignTask(null);
    }

    public void UpdateTask(
        string title,
        string description,
        DateTime? dueAtUtc,
        TaskPriority priority)
    {
        EnsureTaskIsNotCompleted();

        var normalizedTitle = NormalizeRequiredText(title, nameof(title), MaxTitleLength);
        var normalizedDescription = NormalizeRequiredText(description, nameof(description), MaxDescriptionLength);
        var validatedPriority = ValidatePriority(priority);

        if (Title == normalizedTitle &&
            Description == normalizedDescription &&
            DueAtUtc == dueAtUtc &&
            Priority == validatedPriority)
        {
            return;
        }

        Title = normalizedTitle;
        Description = normalizedDescription;
        Priority = validatedPriority;
        if (DueAtUtc != dueAtUtc)
        {
            DueAtUtc = dueAtUtc;
            DeadlineReminderSentAtUtc = null;
        }

        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkDeadlineReminderSent()
    {
        if (!DueAtUtc.HasValue)
        {
            throw new InvalidOperationException("Cannot mark a reminder sent for a task without a due date.");
        }

        DeadlineReminderSentAtUtc ??= DateTime.UtcNow;
    }


    public void MarkCompleted()
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Task is already completed.");
        }

        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        UpdatedAt = CompletedAt;
    }

    private static string NormalizeRequiredText(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters.", parameterName);
        }

        return normalizedValue;
    }

    private static TaskPriority ValidatePriority(TaskPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentException("Priority is not supported.", nameof(priority));
        }

        return priority;
    }

    private void EnsureTaskIsNotCompleted()
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Completed tasks cannot be modified.");
        }
    }
}
