using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Domain.Auth;

public class User
{
    public int Id {get; set;}

    public required string Email {get; set;}

    public required string PasswordHash{get; set;}

    public string Role { get; set; } = "User";

    public int RoleId {get; set;}

    public Role? RoleEntity { get; set; }

    public bool IsActive { get; set; } = true;

}
