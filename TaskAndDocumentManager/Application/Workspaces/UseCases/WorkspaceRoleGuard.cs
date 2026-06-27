using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Workspaces.UseCases;

internal static class WorkspaceRoleGuard
{
    public static WorkspaceMember EnsureCanManageTeams(
        IWorkspaceMemberRepository workspaceMemberRepository,
        Guid workspaceId,
        Guid actorId)
    {
        var membership = workspaceMemberRepository.GetMembership(workspaceId, actorId)
            ?? throw new UnauthorizedAccessException("You do not belong to this workspace.");

        if (!CanManageTeams(membership.Role))
        {
            throw new UnauthorizedAccessException("You do not have permission to manage teams.");
        }

        return membership;
    }

    public static void EnsureIsWorkspaceMember(
        IWorkspaceMemberRepository workspaceMemberRepository,
        Guid workspaceId,
        Guid actorId)
    {
        if (!workspaceMemberRepository.IsMember(workspaceId, actorId))
        {
            throw new UnauthorizedAccessException("You do not belong to this workspace.");
        }
    }

    private static bool CanManageTeams(string workspaceRole)
    {
        return string.Equals(workspaceRole, WorkspaceRoles.Owner, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(workspaceRole, WorkspaceRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(workspaceRole, WorkspaceRoles.Manager, StringComparison.OrdinalIgnoreCase);
    }
}
