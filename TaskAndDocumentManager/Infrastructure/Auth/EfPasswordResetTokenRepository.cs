using Microsoft.EntityFrameworkCore;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Infrastructure.Auth;

public class EfPasswordResetTokenRepository(AppDbContext db) : IPasswordResetTokenRepository
{
    public PasswordResetToken Save(PasswordResetToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        db.PasswordResetTokens.Add(token);
        db.SaveChanges();
        return token;
    }

    public async Task<PasswordResetToken> SaveAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        db.PasswordResetTokens.Add(token);
        await db.SaveChangesAsync(ct);
        return token;
    }

    public PasswordResetToken? GetById(Guid id)
    {
        return db.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefault(token => token.Id == id);
    }

    public Task<PasswordResetToken?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return db.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(token => token.Id == id, ct);
    }

    public PasswordResetToken? GetByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = PasswordResetToken.HashToken(token);
        return db.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefault(existingToken => existingToken.TokenHash == tokenHash);
    }

    public async Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = PasswordResetToken.HashToken(token);
        return await db.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(existingToken => existingToken.TokenHash == tokenHash, ct);
    }

    public PasswordResetToken? GetByUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        return db.PasswordResetTokens
            .AsNoTracking()
            .Where(existingToken => existingToken.UserId == userId)
            .OrderByDescending(existingToken => existingToken.CreatedAtUtc)
            .FirstOrDefault();
    }

    public async Task<PasswordResetToken?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        return await db.PasswordResetTokens
            .AsNoTracking()
            .Where(existingToken => existingToken.UserId == userId)
            .OrderByDescending(existingToken => existingToken.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public PasswordResetToken? GetValidByHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return null;
        }

        return db.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefault(token =>
                token.TokenHash == hash &&
                token.UsedAtUtc == null &&
                token.ExpiresAtUtc > DateTime.UtcNow);
    }

    public Task<PasswordResetToken?> GetValidByHashAsync(string hash, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            return Task.FromResult<PasswordResetToken?>(null);
        }

        return db.PasswordResetTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(token =>
                token.TokenHash == hash &&
                token.UsedAtUtc == null &&
                token.ExpiresAtUtc > DateTime.UtcNow, ct);
    }

    public void MarkUsed(Guid id, DateTime usedAtUtc)
    {
        var token = db.PasswordResetTokens.FirstOrDefault(existingToken => existingToken.Id == id);
        if (token is null)
        {
            throw new KeyNotFoundException("Password reset token was not found.");
        }

        token.MarkUsed(usedAtUtc);
        db.SaveChanges();
    }

    public async Task MarkUsedAsync(Guid id, DateTime usedAtUtc, CancellationToken ct = default)
    {
        var token = await db.PasswordResetTokens.FirstOrDefaultAsync(existingToken => existingToken.Id == id, ct)
            ?? throw new KeyNotFoundException("Password reset token was not found.");

        token.MarkUsed(usedAtUtc);
        await db.SaveChangesAsync(ct);
    }
}
