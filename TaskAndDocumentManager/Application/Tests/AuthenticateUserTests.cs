using Moq;
using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Tests.Auth.UseCases;

public class AuthenticateUserTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRoleCatalog> _roleCatalogMock;
    private readonly Mock<IWorkspaceMemberRepository> _workspaceMemberRepositoryMock;
    private readonly AuthenticateUser _sut;

    public AuthenticateUserTests()
    {
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _roleCatalogMock = new Mock<IRoleCatalog>();
        _workspaceMemberRepositoryMock = new Mock<IWorkspaceMemberRepository>();

        _sut = new AuthenticateUser(
            _auditLogRepositoryMock.Object,
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object,
            _roleCatalogMock.Object,
            _workspaceMemberRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpgradeLegacyHash_WhenPasswordIsValid()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var roleName = "User";
        var email = "person@example.com";
        var password = "Password1";
        var legacyHash = "legacy.hash";
        var upgradedHash = "AQAAAA...";
        var workspaceId = Guid.NewGuid();

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

        _workspaceMemberRepositoryMock
            .Setup(repository => repository.GetDefaultMembershipForUser(userId))
            .Returns(new WorkspaceMember(workspaceId, userId, WorkspaceRoles.Owner));

        _tokenServiceMock
            .Setup(service => service.GenerateToken(userId.ToString(), email, roleName, workspaceId))
            .Returns(new TokenResult
            {
                Token = "token",
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            });

        var result = await _sut.ExecuteAsync(email, password);

        Assert.Equal("token", result.Token);
        Assert.Equal(workspaceId, result.User.WorkspaceId);
        _userRepositoryMock.Verify(repository => repository.Save(
            It.Is<User>(savedUser =>
                savedUser.Id == userId &&
                savedUser.PasswordHash == upgradedHash)),
            Times.Once);
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(auditLog =>
                    auditLog.UserId == userId &&
                    auditLog.Action == AuditActions.UserLoginSucceeded &&
                    auditLog.EntityType == nameof(User) &&
                    auditLog.EntityId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAuditFailedLogin_WhenPasswordIsInvalid()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var email = "person@example.com";
        var password = "wrong-password";

        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = "stored.hash",
            RoleId = roleId,
            IsActive = true
        };

        _userRepositoryMock
            .Setup(repository => repository.GetByEmail(email))
            .Returns(user);

        _passwordHasherMock
            .Setup(hasher => hasher.VerifyPassword(password, user.PasswordHash))
            .Returns(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(email, password));

        Assert.Equal("Invalid email or password.", exception.Message);
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(auditLog =>
                    auditLog.UserId == userId &&
                    auditLog.Action == AuditActions.UserLoginFailed &&
                    auditLog.EntityType == nameof(User) &&
                    auditLog.EntityId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
