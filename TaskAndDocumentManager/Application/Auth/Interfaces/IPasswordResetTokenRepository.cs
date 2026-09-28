using TaskAndDocumentManager.Domain.Auth;

namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface IPasswordResetTokenRepository
{
    PasswordResetToken Save(PasswordResetToken token);
    PasswordResetToken? GetById(Guid id);
    PasswordResetToken? GetByToken(string token);
    void MarkUsed(Guid id, DateTime usedAtUtc);
}
