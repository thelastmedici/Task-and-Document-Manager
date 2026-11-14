using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Auth;

public class SendGridEmailSender : IEmailSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SendGridEmailSender> logger)
    {
        _httpClient = httpClient;
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

        var apiKey = _configuration["Email:SendGrid:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("SendGrid is selected but Email:SendGrid:ApiKey is not configured.");
        }

        var fromEmail = _configuration["Email:From"];
        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("Email:From must be configured when using SendGrid.");
        }

        var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
        var resetLink = $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(resetToken)}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send")
        {
            Content = JsonContent.Create(new SendGridMailRequest(
                new SendGridAddress(fromEmail),
                [new SendGridPersonalization([new SendGridAddress(email)])],
                "Password reset request",
                [new SendGridContent("text/plain", $"Use the following link to reset your password: {resetLink}")] ))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SendGrid rejected password reset email for {Email} with status {StatusCode}: {ResponseBody}",
                email, (int)response.StatusCode, responseBody);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Password reset email sent to {Email} through SendGrid.", email);
    }

    private sealed record SendGridMailRequest(
        [property: JsonPropertyName("from")] SendGridAddress From,
        [property: JsonPropertyName("personalizations")] IReadOnlyList<SendGridPersonalization> Personalizations,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("content")] IReadOnlyList<SendGridContent> Content);

    private sealed record SendGridPersonalization(
        [property: JsonPropertyName("to")] IReadOnlyList<SendGridAddress> To);

    private sealed record SendGridAddress(
        [property: JsonPropertyName("email")] string Email);

    private sealed record SendGridContent(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("value")] string Value);
}
