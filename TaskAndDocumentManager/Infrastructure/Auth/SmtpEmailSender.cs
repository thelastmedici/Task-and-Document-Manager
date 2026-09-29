using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Auth;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(resetToken))
        {
            throw new ArgumentException("Reset token is required.", nameof(resetToken));
        }

        var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
        var resetLink = $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(resetToken)}";

        var host = _configuration["Email:Smtp:Host"];
        var portText = _configuration["Email:Smtp:Port"];
        var fromEmail = _configuration["Email:From"] ?? "noreply@localhost";

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation(
                "SMTP email provider is not configured. Password reset link for {Email}: {ResetLink}",
                email,
                resetLink);
            return;
        }

        var port = int.TryParse(portText, out var parsedPort) ? parsedPort : 587;
        var enableSsl = bool.TryParse(_configuration["Email:Smtp:EnableSsl"], out var parsedSsl) && parsedSsl;
        var username = _configuration["Email:Smtp:Username"];
        var password = _configuration["Email:Smtp:Password"];

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = "Password reset request",
            Body = $"Use the following link to reset your password: {resetLink}",
            IsBodyHtml = false
        };
        message.To.Add(email);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            UseDefaultCredentials = string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password),
            Credentials = string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)
                ? null
                : new NetworkCredential(username, password)
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation(
                "Password reset email sent to {Email}.",
                email);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}.", email);
            throw;
        }
    }
}
