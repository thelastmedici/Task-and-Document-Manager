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

    public Task<PasswordResetToken> SaveAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        return Task.FromResult(Save(token));
    }

    public PasswordResetToken? GetById(Guid id)
    {
        return Tokens.FirstOrDefault(token => token.Id == id);
    }

    public Task<PasswordResetToken?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(GetById(id));
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

    public Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        return Task.FromResult(GetByToken(token));
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

    public Task<PasswordResetToken?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return Task.FromResult(GetByUserId(userId));
    }

    public PasswordResetToken? GetValidByHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return null;
        }

        return Tokens.FirstOrDefault(token =>
            string.Equals(token.TokenHash, hash, StringComparison.OrdinalIgnoreCase) &&
            token.UsedAtUtc is null &&
            token.ExpiresAtUtc > DateTime.UtcNow);
    }

    public Task<PasswordResetToken?> GetValidByHashAsync(string hash, CancellationToken ct = default)
    {
        return Task.FromResult(GetValidByHash(hash));
    }

    public void MarkUsed(Guid id, DateTime usedAtUtc)
    {
        var token = GetById(id) ?? throw new KeyNotFoundException("Password reset token was not found.");
        token.MarkUsed(usedAtUtc);
    }

    public Task MarkUsedAsync(Guid id, DateTime usedAtUtc, CancellationToken ct = default)
    {
        MarkUsed(id, usedAtUtc);
        return Task.CompletedTask;
    }
}
