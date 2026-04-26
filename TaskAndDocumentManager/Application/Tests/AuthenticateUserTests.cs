using Moq;
using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Auth.UseCases;

public class AuthenticateUserTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRoleCatalog> _roleCatalogMock;
    private readonly AuthenticateUser _sut;

    public AuthenticateUserTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _roleCatalogMock = new Mock<IRoleCatalog>();

        _sut = new AuthenticateUser(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _roleCatalogMock.Object);
    }

    [Fact]
    public void Execute_ShouldUpgradeLegacyHash_WhenPasswordIsValid()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var roleName = "User";
        var email = "person@example.com";
        var password = "Password1";
        var legacyHash = "legacy.hash";
        var upgradedHash = "AQAAAA...";

        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = legacyHash,
            RoleId = roleId,
            Role = new Role
            {
                Id = roleId,
                Name = roleName
            },
            IsActive = true
        };

        _userRepositoryMock
            .Setup(repository => repository.GetByEmail(email))
            .Returns(user);

        _passwordHasherMock
            .Setup(hasher => hasher.VerifyPassword(password, legacyHash))
            .Returns(true);

        _passwordHasherMock
            .Setup(hasher => hasher.NeedsRehash(legacyHash))
            .Returns(true);

        _passwordHasherMock
            .Setup(hasher => hasher.HashPassword(password))
            .Returns(upgradedHash);

        _tokenServiceMock
            .Setup(service => service.GenerateToken(userId.ToString(), email, roleName))
            .Returns(new TokenResult
            {
                Token = "token",
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            });

        var result = _sut.Execute(email, password);

        Assert.Equal("token", result.Token);
        _userRepositoryMock.Verify(repository => repository.Save(
            It.Is<User>(savedUser =>
                savedUser.Id == userId &&
                savedUser.PasswordHash == upgradedHash)),
            Times.Once);
    }
}
