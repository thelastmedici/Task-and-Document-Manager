namespace TaskAndDocumentManager.Application.Auth.DTOs;

public sealed class TokenResult
{
    public required string Token { get; init; }
    public DateTime ExpiresAtUtc { get; init; }
}
