using Moq;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Auth.UseCases;

public class ChangeUserRoleTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleCatalog> _roleCatalogMock;
    private readonly ChangeUserRole _sut;

    public ChangeUserRoleTests()
    {
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleCatalogMock = new Mock<IRoleCatalog>();
        _sut = new ChangeUserRole(
            _auditLogRepositoryMock.Object,
            _userRepositoryMock.Object,
            _roleCatalogMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSaveAuditLog_WhenRoleChangeSucceeds()
    {
        var userId = Guid.NewGuid();
        var changedByUserId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            PasswordHash = "hash",
            RoleId = Guid.NewGuid(),
            IsActive = true
        };

        _roleCatalogMock.Setup(catalog => catalog.IsSupportedRole(roleId)).Returns(true);
        _userRepositoryMock.Setup(repository => repository.GetById(userId)).Returns(user);

        await _sut.ExecuteAsync(userId, roleId, changedByUserId, CancellationToken.None);

        _userRepositoryMock.Verify(repository => repository.Save(It.Is<User>(savedUser => savedUser.RoleId == roleId)), Times.Once);
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(auditLog =>
                    auditLog.UserId == changedByUserId &&
                    auditLog.Action == AuditActions.UserRoleChanged &&
                    auditLog.EntityType == nameof(User) &&
                    auditLog.EntityId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotWriteAuditLog_WhenRoleIsInvalid()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));

        Assert.Equal("Role is invalid.", exception.Message);
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
