using TaskAndDocumentManager.Domain.Workspaces;

namespace Tests;

public class WorkspaceTests
{
    [Fact]
    public void Constructor_ShouldCreateWorkspace()
    {
        var createdByUserId = Guid.NewGuid();

        var workspace = new Workspace("  Acme Ltd  ", createdByUserId);

        Assert.NotEqual(Guid.Empty, workspace.Id);
        Assert.Equal("Acme Ltd", workspace.Name);
        Assert.Equal(createdByUserId, workspace.CreatedByUserId);
        Assert.True(workspace.CreatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyName()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Workspace(" ", Guid.NewGuid()));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyCreatedByUserId()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Workspace("Acme Ltd", Guid.Empty));

        Assert.Equal("createdByUserId", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldRejectNameLongerThanMaximum()
    {
        var longName = new string('a', Workspace.MaxNameLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            new Workspace(longName, Guid.NewGuid()));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void WorkspaceMemberConstructor_ShouldCreateMembership()
    {
        var workspaceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var member = new WorkspaceMember(workspaceId, userId, "  admin  ");

        Assert.Equal(workspaceId, member.WorkspaceId);
        Assert.Equal(userId, member.UserId);
        Assert.Equal(WorkspaceRoles.Admin, member.Role);
        Assert.True(member.JoinedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void WorkspaceRoles_ShouldIncludeFutureWorkspaceRoleSet()
    {
        Assert.Contains(WorkspaceRoles.Owner, WorkspaceRoles.All);
        Assert.Contains(WorkspaceRoles.Admin, WorkspaceRoles.All);
        Assert.Contains(WorkspaceRoles.Manager, WorkspaceRoles.All);
        Assert.Contains(WorkspaceRoles.Member, WorkspaceRoles.All);
    }

    [Fact]
    public void WorkspaceMemberConstructor_ShouldRejectEmptyWorkspaceId()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new WorkspaceMember(Guid.Empty, Guid.NewGuid(), WorkspaceRoles.Member));

        Assert.Equal("workspaceId", exception.ParamName);
    }

    [Fact]
    public void WorkspaceMemberConstructor_ShouldRejectEmptyUserId()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new WorkspaceMember(Guid.NewGuid(), Guid.Empty, WorkspaceRoles.Member));

        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void WorkspaceMemberConstructor_ShouldRejectEmptyRole()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new WorkspaceMember(Guid.NewGuid(), Guid.NewGuid(), " "));

        Assert.Equal("role", exception.ParamName);
    }

    [Fact]
    public void WorkspaceMemberConstructor_ShouldRejectUnsupportedRole()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new WorkspaceMember(Guid.NewGuid(), Guid.NewGuid(), "SystemAdmin"));

        Assert.Equal("role", exception.ParamName);
    }

    [Fact]
    public void TeamConstructor_ShouldCreateTeam()
    {
        var workspaceId = Guid.NewGuid();

        var team = new Team(workspaceId, "  Engineering  ");

        Assert.NotEqual(Guid.Empty, team.Id);
        Assert.Equal(workspaceId, team.WorkspaceId);
        Assert.Equal("Engineering", team.Name);
    }

    [Fact]
    public void TeamConstructor_ShouldRejectEmptyWorkspaceId()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Team(Guid.Empty, "Engineering"));

        Assert.Equal("workspaceId", exception.ParamName);
    }

    [Fact]
    public void TeamConstructor_ShouldRejectEmptyName()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Team(Guid.NewGuid(), " "));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void TeamConstructor_ShouldRejectNameLongerThanMaximum()
    {
        var longName = new string('a', Team.MaxNameLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            new Team(Guid.NewGuid(), longName));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void TeamMemberConstructor_ShouldCreateMembership()
    {
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var member = new TeamMember(teamId, userId);

        Assert.Equal(teamId, member.TeamId);
        Assert.Equal(userId, member.UserId);
        Assert.True(member.JoinedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void TeamMemberConstructor_ShouldRejectEmptyTeamId()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new TeamMember(Guid.Empty, Guid.NewGuid()));

        Assert.Equal("teamId", exception.ParamName);
    }

    [Fact]
    public void TeamMemberConstructor_ShouldRejectEmptyUserId()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new TeamMember(Guid.NewGuid(), Guid.Empty));

        Assert.Equal("userId", exception.ParamName);
    }
}
