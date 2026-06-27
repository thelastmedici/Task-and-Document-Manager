using TaskAndDocumentManager.Application.Workspaces.DTOs;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;

namespace TaskAndDocumentManager.Application.Workspaces.UseCases;

public class ListTeams
{
    private readonly ITeamRepository _teamRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public ListTeams(
        ITeamRepository teamRepository,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _teamRepository = teamRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task<IReadOnlyCollection<TeamDto>> ExecuteAsync(
        Guid workspaceId,
        Guid actorId,
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

        WorkspaceRoleGuard.EnsureIsWorkspaceMember(_workspaceMemberRepository, workspaceId, actorId);

        var teams = await _teamRepository.ListByWorkspaceAsync(workspaceId, cancellationToken);
        return teams
            .Select(team => new TeamDto(team.Id, team.WorkspaceId, team.Name))
            .ToList();
    }
}
