using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Domain.Auth;

public class User
{
    public  Guid Id { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    public bool IsActive { get; set; } = true;
}
