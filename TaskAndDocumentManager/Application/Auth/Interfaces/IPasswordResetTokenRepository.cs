using TaskAndDocumentManager.Domain.Auth;

namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface IPasswordResetTokenRepository
{
    PasswordResetToken Save(PasswordResetToken token);
    PasswordResetToken? GetById(Guid id);
    PasswordResetToken? GetByToken(string token);
    PasswordResetToken? GetByUserId(Guid userId);
    void MarkUsed(Guid id, DateTime usedAtUtc);

    Task<PasswordResetToken> SaveAsync(PasswordResetToken token, CancellationToken ct = default);
    Task<PasswordResetToken?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PasswordResetToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<PasswordResetToken?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<PasswordResetToken?> GetValidByHashAsync(string hash, CancellationToken ct = default);
    Task MarkUsedAsync(Guid id, DateTime usedAtUtc, CancellationToken ct = default);
}
