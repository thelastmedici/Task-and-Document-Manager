using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Workspaces.Interfaces;

public interface ITeamRepository
{
    Task AddAsync(Team team, CancellationToken cancellationToken = default);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Team>> ListByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);
    Task AddMemberAsync(TeamMember member, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> HasMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);
}
