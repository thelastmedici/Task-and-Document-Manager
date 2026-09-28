using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Auth;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class RequestPasswordReset
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailSender _emailSender;

    public RequestPasswordReset(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailSender = emailSender;
    }

    public async Task ExecuteAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalizedEmail = email.Trim();
        var user = _userRepository.GetByEmail(normalizedEmail);

        if (user is null)
        {
            return;
        }

        var rawToken = PasswordResetToken.GenerateRawToken();
        var resetToken = new PasswordResetToken(
            user.Id,
            PasswordResetToken.HashToken(rawToken),
            DateTime.UtcNow.AddMinutes(15));

        _passwordResetTokenRepository.Save(resetToken);
        await _emailSender.SendPasswordResetAsync(user.Email, rawToken, cancellationToken);
    }
}
