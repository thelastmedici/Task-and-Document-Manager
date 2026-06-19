using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class GetCurrentUser
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleCatalog _roleCatalog;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public GetCurrentUser(
        IUserRepository userRepository,
        IRoleCatalog roleCatalog,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _userRepository = userRepository;
        _roleCatalog = roleCatalog;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public UserProfile Execute(Guid userId)
    {
        var user = _userRepository.GetById(userId);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var membership = _workspaceMemberRepository.GetDefaultMembershipForUser(user.Id);

        return new UserProfile
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role?.Name ?? _roleCatalog.ResolveName(user.RoleId),
            WorkspaceId = membership?.WorkspaceId ?? Guid.Empty,
            IsActive = user.IsActive
        };
    }
}
