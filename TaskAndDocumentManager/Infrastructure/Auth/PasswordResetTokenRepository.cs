using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Auth;

namespace TaskAndDocumentManager.Infrastructure.Auth;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private static readonly List<PasswordResetToken> Tokens = new();

    public PasswordResetToken Save(PasswordResetToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (token.Id == Guid.Empty)
        {
            token.Id = Guid.NewGuid();
        }

        var existing = Tokens.FirstOrDefault(existingToken => existingToken.Id == token.Id);
        if (existing is not null)
        {
            Tokens.Remove(existing);
        }

        Tokens.Add(token);
        return token;
    }

    public PasswordResetToken? GetById(Guid id)
    {
        return Tokens.FirstOrDefault(token => token.Id == id);
    }

    public PasswordResetToken? GetByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = PasswordResetToken.HashToken(token);
        return Tokens.FirstOrDefault(existingToken =>
            string.Equals(existingToken.TokenHash, tokenHash, StringComparison.OrdinalIgnoreCase));
    }

    public PasswordResetToken? GetByUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        return Tokens
            .Where(existingToken => existingToken.UserId == userId)
            .OrderByDescending(existingToken => existingToken.CreatedAtUtc)
            .FirstOrDefault();
    }

    public void MarkUsed(Guid id, DateTime usedAtUtc)
    {
        var token = GetById(id) ?? throw new KeyNotFoundException("Password reset token was not found.");
        token.MarkUsed(usedAtUtc);
    }
}
