using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Controllers;
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
    public async Task RequestPasswordReset_ShouldNotCreateTokenOrSendEmail_WhenUserIsInactive()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive@example.com",
            PasswordHash = "hash",
            RoleId = Guid.NewGuid(),
            IsActive = false
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
            repository => repository.Save(It.IsAny<PasswordResetToken>()),
            Times.Never);

        emailSenderMock.Verify(
            sender => sender.SendPasswordResetAsync(user.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestPasswordReset_ShouldReturnWithoutSendingEmail_WhenUserDoesNotExist()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var resetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        var emailSenderMock = new Mock<IEmailSender>();

        userRepositoryMock
            .Setup(repository => repository.GetByEmail("missing@example.com"))
            .Returns((User?)null);

        var sut = new RequestPasswordReset(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            emailSenderMock.Object);

        await sut.ExecuteAsync("missing@example.com", CancellationToken.None);

        resetTokenRepositoryMock.Verify(
            repository => repository.Save(It.IsAny<PasswordResetToken>()),
            Times.Never);

        emailSenderMock.Verify(
            sender => sender.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnFalse_WhenTokenIsExpired()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "person@example.com",
            PasswordHash = "old-hash",
            RoleId = Guid.NewGuid(),
            IsActive = true
        };

        var expiredToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "expired-hash",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5),
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
            .Setup(repository => repository.GetByToken("expired-token"))
            .Returns(expiredToken);

        var sut = new ResetPassword(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            passwordHasherMock.Object,
            passwordValidatorMock.Object);

        var result = await sut.ExecuteAsync("expired-token", "NewPassword1!");

        Assert.False(result);
        passwordHasherMock.Verify(
            hasher => hasher.HashPassword(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnFalse_WhenTokenHasAlreadyBeenUsed()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "person@example.com",
            PasswordHash = "old-hash",
            RoleId = Guid.NewGuid(),
            IsActive = true
        };

        var usedToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "used-hash",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            UsedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        var userRepositoryMock = new Mock<IUserRepository>();
        var resetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var passwordValidatorMock = new Mock<IPasswordValidator>();

        resetTokenRepositoryMock
            .Setup(repository => repository.GetByToken("used-token"))
            .Returns(usedToken);

        var sut = new ResetPassword(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            passwordHasherMock.Object,
            passwordValidatorMock.Object);

        var result = await sut.ExecuteAsync("used-token", "NewPassword1!");

        Assert.False(result);
        userRepositoryMock.Verify(
            repository => repository.Save(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ShouldThrowArgumentException_WhenPasswordIsWeak()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var resetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var passwordValidatorMock = new Mock<IPasswordValidator>();

        resetTokenRepositoryMock
            .Setup(repository => repository.GetByToken("valid-token"))
            .Returns(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TokenHash = "valid-token-hash",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
                UsedAtUtc = null
            });

        userRepositoryMock
            .Setup(repository => repository.GetById(It.IsAny<Guid>()))
            .Returns(new User
            {
                Id = Guid.NewGuid(),
                Email = "person@example.com",
                PasswordHash = "old-hash",
                RoleId = Guid.NewGuid(),
                IsActive = true
            });

        passwordValidatorMock
            .Setup(validator => validator.IsPasswordStrong("weak"))
            .Returns(false);

        var sut = new ResetPassword(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            passwordHasherMock.Object,
            passwordValidatorMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.ExecuteAsync("valid-token", "weak"));
        passwordHasherMock.Verify(
            hasher => hasher.HashPassword(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPassword_ShouldUpdatePasswordAndInvalidateToken_WhenEmailIsProvidedAndTokenIsValid()
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
            .Setup(repository => repository.GetByEmail(user.Email))
            .Returns(user);

        userRepositoryMock
            .Setup(repository => repository.GetById(user.Id))
            .Returns(user);

        resetTokenRepositoryMock
            .Setup(repository => repository.GetByUserId(user.Id))
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

        var result = await sut.ExecuteAsync(null, user.Email, "NewPassword1!");

        Assert.True(result);
        userRepositoryMock.Verify(
            repository => repository.Save(
                It.Is<User>(savedUser => savedUser.Id == user.Id && savedUser.PasswordHash == "new-hash")),
            Times.Once);

        resetTokenRepositoryMock.Verify(
            repository => repository.MarkUsed(resetToken.Id, It.IsAny<DateTime>()),
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

    [Fact]
    public async Task AuthController_ForgotPassword_ShouldReturnAccepted_WhenRequestIsValid()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var resetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        var emailSenderMock = new Mock<IEmailSender>();
        var requestPasswordReset = new RequestPasswordReset(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            emailSenderMock.Object);

        var controller = new AuthController(
            null!, null!, null!, null!, null!, null!, null!, null!, requestPasswordReset, null!);

        var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "person@example.com" }, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal(202, accepted.StatusCode);
    }

    [Fact]
    public async Task AuthController_ResetPassword_ShouldReturnUnauthorized_WhenTokenIsInvalid()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var resetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var passwordValidatorMock = new Mock<IPasswordValidator>();

        resetTokenRepositoryMock
            .Setup(repository => repository.GetByToken("bad-token"))
            .Returns((PasswordResetToken?)null);

        var resetUseCase = new ResetPassword(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            passwordHasherMock.Object,
            passwordValidatorMock.Object);

        var controller = new AuthController(
            null!, null!, null!, null!, null!, null!, null!, null!, null!, resetUseCase);

        var result = await controller.ResetPassword(new ResetPasswordRequest
        {
            Token = "bad-token",
            NewPassword = "StrongPassword1!"
        }, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(401, unauthorized.StatusCode);
    }

    [Fact]
    public async Task AuthController_ResetPassword_ShouldReturnNotFound_WhenEmailDoesNotExist()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var resetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var passwordValidatorMock = new Mock<IPasswordValidator>();

        userRepositoryMock
            .Setup(repository => repository.GetByEmail("missing@example.com"))
            .Returns((User?)null);

        var resetUseCase = new ResetPassword(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            passwordHasherMock.Object,
            passwordValidatorMock.Object);

        var controller = new AuthController(
            null!, null!, null!, null!, null!, null!, null!, null!, null!, resetUseCase);

        var result = await controller.ResetPassword(new ResetPasswordRequest
        {
            Email = "missing@example.com",
            NewPassword = "StrongPassword1!"
        }, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task AuthController_ResetPassword_ShouldReturnBadRequest_WhenPasswordIsWeak()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "person@example.com",
            PasswordHash = "old-hash",
            RoleId = Guid.NewGuid(),
            IsActive = true
        };

        var userRepositoryMock = new Mock<IUserRepository>();
        var resetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var passwordValidatorMock = new Mock<IPasswordValidator>();

        resetTokenRepositoryMock
            .Setup(repository => repository.GetByToken("valid-token"))
            .Returns(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "valid-token-hash",
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
                UsedAtUtc = null
            });

        userRepositoryMock
            .Setup(repository => repository.GetById(user.Id))
            .Returns(user);

        passwordValidatorMock
            .Setup(validator => validator.IsPasswordStrong("WeakPass"))
            .Returns(false);

        var resetUseCase = new ResetPassword(
            userRepositoryMock.Object,
            resetTokenRepositoryMock.Object,
            passwordHasherMock.Object,
            passwordValidatorMock.Object);

        var controller = new AuthController(
            null!, null!, null!, null!, null!, null!, null!, null!, null!, resetUseCase);

        var result = await controller.ResetPassword(new ResetPasswordRequest
        {
            Token = "valid-token",
            NewPassword = "WeakPass"
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }
}
