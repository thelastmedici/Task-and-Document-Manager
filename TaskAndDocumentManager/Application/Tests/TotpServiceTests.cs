using TaskAndDocumentManager.Application.Auth.Services;

namespace TaskAndDocumentManager.Application.Tests.Auth.Services;

public class TotpServiceTests
{
    private readonly TotpService _sut = new();

    [Fact]
    public void GenerateSetup_ShouldReturnOtpAuthUriWithIssuerAndLabel()
    {
        const string email = "person@example.com";

        var setup = _sut.GenerateSetup(email);

        Assert.False(string.IsNullOrWhiteSpace(setup.Secret));
        Assert.StartsWith("otpauth://totp/", setup.OtpAuthUri, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TaskAndDocumentManager", setup.OtpAuthUri, StringComparison.OrdinalIgnoreCase);
        Assert.Contains( Uri.EscapeDataString(email), setup.OtpAuthUri, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(32, setup.Secret.Length);
    }

    [Fact]
    public void VerifyCode_ShouldAcceptCurrentTotpCode()
    {
        var setup = _sut.GenerateSetup("person@example.com");
        var now = DateTime.UtcNow;
        var code = _sut.GenerateCode(setup.Secret, now);

        Assert.True(_sut.VerifyCode(setup.Secret, code, now));
    }

    [Fact]
    public void GenerateRecoveryCodes_ShouldCreateTenUniqueCodes()
    {
        var codes = _sut.GenerateRecoveryCodes();

        Assert.Equal(10, codes.Count);
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(codes, code => Assert.Matches("^[A-Z0-9]{10}$", code));
    }
}
