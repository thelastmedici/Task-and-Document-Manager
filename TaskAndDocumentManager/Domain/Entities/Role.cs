namespace TaskAndDocumentManager.Domain.Entities;

public class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public void UpdateTimestamp()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
