namespace TaskAndDocumentManager.Domain.Entities;

public class TaskItem
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 4000;

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid? AssignedUserId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    protected TaskItem() { }

    public TaskItem(string title, string description, Guid createdByUserId)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Created by user ID is required.", nameof(createdByUserId));
        }

        Id = Guid.NewGuid();
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTime.UtcNow;
        Title = NormalizeRequiredText(title, nameof(title), MaxTitleLength);
        Description = NormalizeRequiredText(description, nameof(description), MaxDescriptionLength);
        IsCompleted = false;
    }

    public void AssignTask(Guid userId)
    {
        EnsureTaskIsNotCompleted();

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (AssignedUserId == userId)
        {
            return;
        }

        AssignedUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTask(string title, string description)
    {
        EnsureTaskIsNotCompleted();

        var normalizedTitle = NormalizeRequiredText(title, nameof(title), MaxTitleLength);
        var normalizedDescription = NormalizeRequiredText(description, nameof(description), MaxDescriptionLength);

        if (Title == normalizedTitle && Description == normalizedDescription)
        {
            return;
        }

        Title = normalizedTitle;
        Description = normalizedDescription;
        UpdatedAt = DateTime.UtcNow;
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

    private void EnsureTaskIsNotCompleted()
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Completed tasks cannot be modified.");
        }
    }
}
