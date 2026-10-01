using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Auth;

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
        => ExecuteAsync(token, null, newPassword, cancellationToken);

    public Task<bool> ExecuteAsync(string? token, string? email, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("A valid reset token or email is required.", nameof(token));
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password is required.", nameof(newPassword));
        }

        if (!_passwordValidator.IsPasswordStrong(newPassword))
        {
            throw new ArgumentException("Password is not strong enough.", nameof(newPassword));
        }

        PasswordResetToken? resetToken = null;

        if (!string.IsNullOrWhiteSpace(token))
        {
            resetToken = _passwordResetTokenRepository.GetByToken(token);
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            var user = _userRepository.GetByEmail(email.Trim());
            if (user is null)
            {
                return Task.FromResult(false);
            }

            resetToken = _passwordResetTokenRepository.GetByUserId(user.Id);
        }

        if (resetToken is null || resetToken.IsExpired || resetToken.IsUsed)
        {
            return Task.FromResult(false);
        }

        var userByToken = _userRepository.GetById(resetToken.UserId);
        if (userByToken is null)
        {
            return Task.FromResult(false);
        }

        userByToken.PasswordHash = _passwordHasher.HashPassword(newPassword);
        _userRepository.Save(userByToken);
        _passwordResetTokenRepository.MarkUsed(resetToken.Id, DateTime.UtcNow);

        return Task.FromResult(true);
    }
}
