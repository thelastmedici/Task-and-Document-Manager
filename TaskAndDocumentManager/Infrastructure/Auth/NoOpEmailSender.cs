using Microsoft.Extensions.Logging;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Auth;

public class NoOpEmailSender : IEmailSender
{
    private readonly ILogger<NoOpEmailSender> _logger;

    public NoOpEmailSender(ILogger<NoOpEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        _logger.LogWarning(
            "Password reset email was not actually sent for {Email}. Configure a real email sender before enabling this in production.",
            email);

        return Task.CompletedTask;
    }
}
