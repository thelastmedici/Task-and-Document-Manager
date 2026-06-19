using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Infrastructure.Workspaces;

public class InMemoryWorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private static readonly List<WorkspaceMember> Members = new();

    public WorkspaceMember Add(WorkspaceMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        var existingMember = Members.FirstOrDefault(existing =>
            existing.WorkspaceId == member.WorkspaceId &&
            existing.UserId == member.UserId);

        if (existingMember is not null)
        {
            Members.Remove(existingMember);
        }

        Members.Add(member);
        return member;
    }

    public WorkspaceMember? GetDefaultMembershipForUser(Guid userId)
    {
        return Members
            .Where(member => member.UserId == userId)
            .OrderBy(member => member.JoinedAtUtc)
            .FirstOrDefault();
    }

    public bool IsMember(Guid workspaceId, Guid userId)
    {
        return Members.Any(member =>
            member.WorkspaceId == workspaceId &&
            member.UserId == userId);
    }
}
