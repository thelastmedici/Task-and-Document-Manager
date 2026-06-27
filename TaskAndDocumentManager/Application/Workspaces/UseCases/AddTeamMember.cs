using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Workspaces.UseCases;

public class AddTeamMember
{
    private readonly ITeamRepository _teamRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public AddTeamMember(
        ITeamRepository teamRepository,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _teamRepository = teamRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task ExecuteAsync(
        Guid workspaceId,
        Guid actorId,
        Guid teamId,
        Guid userId,
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

        if (teamId == Guid.Empty)
        {
            throw new ArgumentException("Team ID is required.", nameof(teamId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        WorkspaceRoleGuard.EnsureCanManageTeams(_workspaceMemberRepository, workspaceId, actorId);

        var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken)
            ?? throw new KeyNotFoundException("Team not found.");

        if (team.WorkspaceId != workspaceId)
        {
            throw new KeyNotFoundException("Team not found.");
        }

        if (!_workspaceMemberRepository.IsMember(workspaceId, userId))
        {
            throw new InvalidOperationException("User must belong to this workspace before joining a team.");
        }

        await _teamRepository.AddMemberAsync(new TeamMember(teamId, userId), cancellationToken);
    }
}
