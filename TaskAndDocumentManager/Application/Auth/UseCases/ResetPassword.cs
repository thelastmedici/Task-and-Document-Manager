using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class ResetPassword
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;

    public ResetPassword(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
    }

    public Task<bool> ExecuteAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Reset token is required.", nameof(token));
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password is required.", nameof(newPassword));
        }

        if (!_passwordValidator.IsPasswordStrong(newPassword))
        {
            throw new ArgumentException("Password is not strong enough.", nameof(newPassword));
        }

        var resetToken = _passwordResetTokenRepository.GetByToken(token);
        if (resetToken is null || resetToken.IsExpired || resetToken.IsUsed)
        {
            return Task.FromResult(false);
        }

        var user = _userRepository.GetById(resetToken.UserId);
        if (user is null)
        {
            return Task.FromResult(false);
        }

        user.PasswordHash = _passwordHasher.HashPassword(newPassword);
        _userRepository.Save(user);
        _passwordResetTokenRepository.MarkUsed(resetToken.Id, DateTime.UtcNow);

        return Task.FromResult(true);
    }
}
