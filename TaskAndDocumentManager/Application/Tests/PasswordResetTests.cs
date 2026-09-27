using Moq;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Domain.Auth;

namespace TaskAndDocumentManager.Application.Tests.Auth.UseCases;

public class PasswordResetTests
{
    [Fact]
    public async Task RequestPasswordReset_ShouldCreateTokenAndSendEmail_WhenUserExists()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "person@example.com",
            PasswordHash = "hash",
            RoleId = Guid.NewGuid(),
            IsActive = true
        };

        var userRepositoryMock = new Mock<IUserRepository>();
        var resetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        var emailSenderMock = new Mock<IEmailSender>();

        userRepositoryMock
            .Setup(repository => repository.GetByEmail(user.Email))
            .Returns(user);

        var sut = new RequestPasswordReset(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            emailSenderMock.Object);

        await sut.ExecuteAsync(user.Email, CancellationToken.None);

        resetTokenRepositoryMock.Verify(
            repository => repository.Save(
                It.Is<PasswordResetToken>(token =>
                    token.UserId == user.Id &&
                    token.ExpiresAtUtc > DateTime.UtcNow)),
            Times.Once);

        emailSenderMock.Verify(
            sender => sender.SendPasswordResetAsync(user.Email, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ShouldUpdatePasswordAndInvalidateToken_WhenTokenIsValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "person@example.com",
            PasswordHash = "old-hash",
            RoleId = Guid.NewGuid(),
            IsActive = true
        };

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "valid-token-hash",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            UsedAtUtc = null
        };

        var userRepositoryMock = new Mock<IUserRepository>();
        var resetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var passwordValidatorMock = new Mock<IPasswordValidator>();

        userRepositoryMock
            .Setup(repository => repository.GetById(user.Id))
            .Returns(user);

        resetTokenRepositoryMock
            .Setup(repository => repository.GetByToken("valid-token"))
            .Returns(resetToken);

        passwordValidatorMock
            .Setup(validator => validator.IsPasswordStrong("NewPassword1!"))
            .Returns(true);

        passwordHasherMock
            .Setup(hasher => hasher.HashPassword("NewPassword1!"))
            .Returns("new-hash");

        var sut = new ResetPassword(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            passwordHasherMock.Object,
            passwordValidatorMock.Object);

        var result = await sut.ExecuteAsync("valid-token", "NewPassword1!");

        Assert.True(result);
        userRepositoryMock.Verify(
            repository => repository.Save(
                It.Is<User>(savedUser => savedUser.Id == user.Id && savedUser.PasswordHash == "new-hash")),
            Times.Once);

        resetTokenRepositoryMock.Verify(
            repository => repository.MarkUsed(resetToken.Id, It.IsAny<DateTime>()),
            Times.Once);
    }
}
