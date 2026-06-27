using Moq;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.UseCases;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Tests.Workspaces.UseCases;

public class TeamUseCaseTests
{
    private readonly Mock<ITeamRepository> _teamRepositoryMock;
    private readonly Mock<IWorkspaceMemberRepository> _workspaceMemberRepositoryMock;

    public TeamUseCaseTests()
    {
        _teamRepositoryMock = new Mock<ITeamRepository>();
        _workspaceMemberRepositoryMock = new Mock<IWorkspaceMemberRepository>();
    }

    [Fact]
    public async Task CreateTeam_ShouldCreateTeam_WhenActorCanManageTeams()
    {
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var useCase = new CreateTeam(
            _teamRepositoryMock.Object,
            _workspaceMemberRepositoryMock.Object);

        _workspaceMemberRepositoryMock
            .Setup(repository => repository.GetMembership(workspaceId, actorId))
            .Returns(new WorkspaceMember(workspaceId, actorId, WorkspaceRoles.Manager));

        var result = await useCase.ExecuteAsync(workspaceId, actorId, " Engineering ");

        Assert.Equal(workspaceId, result.WorkspaceId);
        Assert.Equal("Engineering", result.Name);
        _teamRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Team>(team =>
                    team.WorkspaceId == workspaceId &&
                    team.Name == "Engineering"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTeam_ShouldRejectWorkspaceMemberWithoutManagerRole()
    {
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var useCase = new CreateTeam(
            _teamRepositoryMock.Object,
            _workspaceMemberRepositoryMock.Object);

        _workspaceMemberRepositoryMock
            .Setup(repository => repository.GetMembership(workspaceId, actorId))
            .Returns(new WorkspaceMember(workspaceId, actorId, WorkspaceRoles.Member));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(workspaceId, actorId, "Engineering"));

        _teamRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ListTeams_ShouldReturnTeams_WhenActorBelongsToWorkspace()
    {
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var team = new Team(workspaceId, "Engineering");
        var useCase = new ListTeams(
            _teamRepositoryMock.Object,
            _workspaceMemberRepositoryMock.Object);

        _workspaceMemberRepositoryMock
            .Setup(repository => repository.IsMember(workspaceId, actorId))
            .Returns(true);
        _teamRepositoryMock
            .Setup(repository => repository.ListByWorkspaceAsync(workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { team });

        var result = await useCase.ExecuteAsync(workspaceId, actorId);

        var item = Assert.Single(result);
        Assert.Equal(team.Id, item.Id);
        Assert.Equal(team.WorkspaceId, item.WorkspaceId);
        Assert.Equal(team.Name, item.Name);
    }

    [Fact]
    public async Task AddTeamMember_ShouldAddUser_WhenActorCanManageTeamsAndUserBelongsToWorkspace()
    {
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var team = new Team(workspaceId, "Engineering");
        var useCase = new AddTeamMember(
            _teamRepositoryMock.Object,
            _workspaceMemberRepositoryMock.Object);

        _workspaceMemberRepositoryMock
            .Setup(repository => repository.GetMembership(workspaceId, actorId))
            .Returns(new WorkspaceMember(workspaceId, actorId, WorkspaceRoles.Admin));
        _workspaceMemberRepositoryMock
            .Setup(repository => repository.IsMember(workspaceId, userId))
            .Returns(true);
        _teamRepositoryMock
            .Setup(repository => repository.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        await useCase.ExecuteAsync(workspaceId, actorId, team.Id, userId);

        _teamRepositoryMock.Verify(
            repository => repository.AddMemberAsync(
                It.Is<TeamMember>(member =>
                    member.TeamId == team.Id &&
                    member.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddTeamMember_ShouldRejectUserOutsideWorkspace()
    {
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var team = new Team(workspaceId, "Engineering");
        var useCase = new AddTeamMember(
            _teamRepositoryMock.Object,
            _workspaceMemberRepositoryMock.Object);

        _workspaceMemberRepositoryMock
            .Setup(repository => repository.GetMembership(workspaceId, actorId))
            .Returns(new WorkspaceMember(workspaceId, actorId, WorkspaceRoles.Owner));
        _workspaceMemberRepositoryMock
            .Setup(repository => repository.IsMember(workspaceId, userId))
            .Returns(false);
        _teamRepositoryMock
            .Setup(repository => repository.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(workspaceId, actorId, team.Id, userId));

        _teamRepositoryMock.Verify(
            repository => repository.AddMemberAsync(It.IsAny<TeamMember>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveTeamMember_ShouldRemoveUser_WhenActorCanManageTeams()
    {
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var team = new Team(workspaceId, "Engineering");
        var useCase = new RemoveTeamMember(
            _teamRepositoryMock.Object,
            _workspaceMemberRepositoryMock.Object);

        _workspaceMemberRepositoryMock
            .Setup(repository => repository.GetMembership(workspaceId, actorId))
            .Returns(new WorkspaceMember(workspaceId, actorId, WorkspaceRoles.Owner));
        _teamRepositoryMock
            .Setup(repository => repository.GetByIdAsync(team.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(team);

        await useCase.ExecuteAsync(workspaceId, actorId, team.Id, userId);

        _teamRepositoryMock.Verify(
            repository => repository.RemoveMemberAsync(team.Id, userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
