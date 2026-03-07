namespace TaskAndDocumentManager.Application.Auth.DTOs;

public sealed class UserProfile
{
    public int Id { get; init; }
    public required string Email { get; init; }
    public string Role { get; init; } = "User";
    public bool IsActive { get; init; } = true;
}
