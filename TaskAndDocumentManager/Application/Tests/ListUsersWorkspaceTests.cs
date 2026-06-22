using Moq;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Auth;

namespace Tests;

public class ListUsersWorkspaceTests
{
    [Fact]
    public void Execute_ShouldReturnOnlyUsersInRequestedWorkspace()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var roleCatalogMock = new Mock<IRoleCatalog>();
        var workspaceMemberRepositoryMock = new Mock<IWorkspaceMemberRepository>();
        var workspaceId = Guid.NewGuid();
        var member = CreateUser();
        var otherUser = CreateUser();

        userRepositoryMock
            .Setup(repository => repository.GetAll())
            .Returns(new[] { member, otherUser });
        workspaceMemberRepositoryMock
            .Setup(repository => repository.GetUserIdsForWorkspace(workspaceId))
            .Returns(new[] { member.Id });
        roleCatalogMock
            .Setup(catalog => catalog.ResolveName(It.IsAny<Guid>()))
            .Returns("User");

        var sut = new ListUsers(
            userRepositoryMock.Object,
            roleCatalogMock.Object,
            workspaceMemberRepositoryMock.Object);

        var users = sut.Execute(workspaceId);

        var user = Assert.Single(users);
        Assert.Equal(member.Id, user.Id);
        Assert.Equal(workspaceId, user.WorkspaceId);
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            RoleId = Guid.NewGuid(),
            IsActive = true
        };
    }
}
