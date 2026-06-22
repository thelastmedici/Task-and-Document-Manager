using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class ChangeUserRole
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleCatalog _roleCatalog;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public ChangeUserRole(
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        IRoleCatalog roleCatalog,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _roleCatalog = roleCatalog;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task ExecuteAsync(
        Guid userId,
        Guid roleId,
        Guid changedByUserId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (changedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Changed by user ID is required.", nameof(changedByUserId));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        if (!_roleCatalog.IsSupportedRole(roleId))
        {
            throw new ArgumentException("Role is invalid.");
        }

        if (!_workspaceMemberRepository.IsMember(workspaceId, changedByUserId))
        {
            throw new UnauthorizedAccessException("You do not belong to this workspace.");
        }

        if (!_workspaceMemberRepository.IsMember(workspaceId, userId))
        {
            throw new KeyNotFoundException("User not found.");
        }

        var user = _userRepository.GetById(userId);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.RoleId = roleId;
        user.Role = null;

        _userRepository.Save(user);
        await _auditLogRepository.AddAsync(
            new AuditLog(
                changedByUserId,
                AuditActions.UserRoleChanged,
                nameof(User),
                userId,
                workspaceId),
            cancellationToken);
    }
}
