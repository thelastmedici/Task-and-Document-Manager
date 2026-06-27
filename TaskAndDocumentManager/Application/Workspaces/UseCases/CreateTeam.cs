using TaskAndDocumentManager.Application.Workspaces.DTOs;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Workspaces.UseCases;

public class CreateTeam
{
    private readonly ITeamRepository _teamRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public CreateTeam(
        ITeamRepository teamRepository,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _teamRepository = teamRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task<TeamDto> ExecuteAsync(
        Guid workspaceId,
        Guid actorId,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("Actor ID is required.", nameof(actorId));
        }

        WorkspaceRoleGuard.EnsureCanManageTeams(_workspaceMemberRepository, workspaceId, actorId);

        var team = new Team(workspaceId, name);
        await _teamRepository.AddAsync(team, cancellationToken);

        return ToDto(team);
    }

    private static TeamDto ToDto(Team team)
    {
        return new TeamDto(team.Id, team.WorkspaceId, team.Name);
    }
}
