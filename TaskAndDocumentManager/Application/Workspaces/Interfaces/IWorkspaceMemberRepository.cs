using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Workspaces.Interfaces;

public interface IWorkspaceMemberRepository
{
    WorkspaceMember Add(WorkspaceMember member);
    WorkspaceMember? GetDefaultMembershipForUser(Guid userId);
    bool IsMember(Guid workspaceId, Guid userId);
}
