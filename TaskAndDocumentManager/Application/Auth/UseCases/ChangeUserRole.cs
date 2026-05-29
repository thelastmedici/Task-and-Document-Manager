using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class ChangeUserRole
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleCatalog _roleCatalog;

    public ChangeUserRole(
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        IRoleCatalog roleCatalog)
    {
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _roleCatalog = roleCatalog;
    }

    public async Task ExecuteAsync(Guid userId, Guid roleId, Guid changedByUserId, CancellationToken cancellationToken = default)
    {
        if (changedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Changed by user ID is required.", nameof(changedByUserId));
        }

        if (!_roleCatalog.IsSupportedRole(roleId))
        {
            throw new ArgumentException("Role is invalid.");
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
                userId),
            cancellationToken);
    }
}