using TaskAndDocumentManager.Domain.Auth;

namespace TaskAndDocumentManager.Application.Tests.Auth.UseCases;

internal static class PasswordResetTokenFactory
{
    public static PasswordResetToken Create(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime? usedAtUtc = null)
    {
        var token = (PasswordResetToken)Activator.CreateInstance(
            typeof(PasswordResetToken), nonPublic: true)!;

        Set(token, nameof(PasswordResetToken.Id), id);
        Set(token, nameof(PasswordResetToken.UserId), userId);
        Set(token, nameof(PasswordResetToken.TokenHash), tokenHash);
        Set(token, nameof(PasswordResetToken.ExpiresAtUtc), expiresAtUtc);
        Set(token, nameof(PasswordResetToken.UsedAtUtc), usedAtUtc);
        return token;
    }

    private static void Set(PasswordResetToken token, string property, object? value) =>
        typeof(PasswordResetToken).GetProperty(property)!.SetValue(token, value);
}