using Moq;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Auth.UseCases;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Workspaces;

namespace Tests;

public class RegisterUserWorkspaceTests
{
    [Fact]
    public void Execute_ShouldCreateWorkspaceAndOwnerMembership()
    {
        var userRepositoryMock = new Mock<IUserRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var emailValidatorMock = new Mock<IEmailValidator>();
        var passwordValidatorMock = new Mock<IPasswordValidator>();
        var roleCatalogMock = new Mock<IRoleCatalog>();
        var workspaceRepositoryMock = new Mock<IWorkspaceRepository>();
        var workspaceMemberRepositoryMock = new Mock<IWorkspaceMemberRepository>();
        var userRoleId = Guid.NewGuid();

        emailValidatorMock
            .Setup(validator => validator.IsValidEmail("owner@example.com"))
            .Returns(true);
        passwordValidatorMock
            .Setup(validator => validator.IsPasswordStrong("Password1"))
            .Returns(true);
        passwordHasherMock
            .Setup(hasher => hasher.HashPassword("Password1"))
            .Returns("hashed-password");
        roleCatalogMock
            .SetupGet(catalog => catalog.UserRoleId)
            .Returns(userRoleId);
        userRepositoryMock
            .Setup(repository => repository.Save(It.IsAny<User>()))
            .Returns((User user) => user);

        var sut = new RegisterUser(
            userRepositoryMock.Object,
            passwordHasherMock.Object,
            emailValidatorMock.Object,
            passwordValidatorMock.Object,
            roleCatalogMock.Object,
            workspaceRepositoryMock.Object,
            workspaceMemberRepositoryMock.Object);

        sut.Execute("owner@example.com", "Password1");

        userRepositoryMock.Verify(repository => repository.Save(
            It.Is<User>(user =>
                user.Id != Guid.Empty &&
                user.Email == "owner@example.com" &&
                user.RoleId == userRoleId)), Times.Once);
        workspaceRepositoryMock.Verify(repository => repository.Add(
            It.Is<Workspace>(workspace =>
                workspace.Name == "owner's Workspace" &&
                workspace.CreatedByUserId != Guid.Empty)), Times.Once);
        workspaceMemberRepositoryMock.Verify(repository => repository.Add(
            It.Is<WorkspaceMember>(member =>
                member.WorkspaceId != Guid.Empty &&
                member.UserId != Guid.Empty &&
                member.Role == WorkspaceRoles.Owner)), Times.Once);
    }
}
