using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Infrastructure.Workspaces;

public class InMemoryTeamRepository : ITeamRepository
{
    private static readonly List<Team> Teams = new();
    private static readonly List<TeamMember> Members = new();

    public Task AddAsync(Team team, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(team);

        if (Teams.Any(existingTeam =>
            existingTeam.WorkspaceId == team.WorkspaceId &&
            string.Equals(existingTeam.Name, team.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("A team with this name already exists in the workspace.");
        }

        Teams.Add(team);
        return Task.CompletedTask;
    }

    public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Teams.FirstOrDefault(team => team.Id == id));
    }

    public Task<IReadOnlyCollection<Team>> ListByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var teams = Teams
            .Where(team => team.WorkspaceId == workspaceId)
            .OrderBy(team => team.Name)
            .ToList();

        return Task.FromResult((IReadOnlyCollection<Team>)teams);
    }

    public Task AddMemberAsync(TeamMember member, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (!Members.Any(existingMember =>
            existingMember.TeamId == member.TeamId &&
            existingMember.UserId == member.UserId))
        {
            Members.Add(member);
        }

        return Task.CompletedTask;
    }

    public Task RemoveMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existingMember = Members.FirstOrDefault(member =>
            member.TeamId == teamId &&
            member.UserId == userId);

        if (existingMember is not null)
        {
            Members.Remove(existingMember);
        }

        return Task.CompletedTask;
    }

    public Task<bool> HasMemberAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var hasMember = Members.Any(member =>
            member.TeamId == teamId &&
            member.UserId == userId);

        return Task.FromResult(hasMember);
    }
}
