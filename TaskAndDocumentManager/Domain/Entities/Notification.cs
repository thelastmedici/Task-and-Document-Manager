namespace TaskAndDocumentManager.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    protected Notification()
    {
    }

    public Notification(
        Guid userId,
        string title,
        string message)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        UserId = userId;
        Title = title.Trim();
        Message = message.Trim();
        IsRead = false;
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
