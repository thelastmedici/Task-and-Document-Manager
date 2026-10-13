using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Domain.Auth;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public void UpdateTimestamp()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
