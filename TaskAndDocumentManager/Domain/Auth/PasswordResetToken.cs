using System.Security.Cryptography;
using System.Text;

namespace TaskAndDocumentManager.Domain.Auth;

public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? UsedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public PasswordResetToken()
    {
    }

    public PasswordResetToken(Guid userId, string tokenHash, DateTime expiresAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        if (expiresAtUtc <= DateTime.UtcNow)
        {
            throw new ArgumentException("Expiration time must be in the future.", nameof(expiresAtUtc));
        }

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public bool IsUsed => UsedAtUtc.HasValue;

    public void MarkUsed(DateTime usedAtUtc)
    {
        if (UsedAtUtc.HasValue)
        {
            return;
        }

        UsedAtUtc = usedAtUtc;
    }

    public static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes);
    }

    public static string HashToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException("Reset token is required.", nameof(rawToken));
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken.Trim()));
        return Convert.ToHexString(bytes);
    }
}
