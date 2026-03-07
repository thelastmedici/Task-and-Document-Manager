namespace TaskAndDocumentManager.Application.Auth.DTOs;

public sealed class AuthResponse
{
    public required string Token { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
    public required UserProfile User { get; init; }
}
