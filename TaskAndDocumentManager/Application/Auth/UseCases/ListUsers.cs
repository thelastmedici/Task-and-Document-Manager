using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Auth;

namespace TaskAndDocumentManager.Application.Auth.UseCases;


public class ListUsers
{
    private readonly IUserRepository _userRepository;

    private readonly IRoleCatalog _roleCatalog;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;


    public ListUsers(
        IUserRepository userRepository,
        IRoleCatalog roleCatalog,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _userRepository = userRepository;
        _roleCatalog = roleCatalog;
        _workspaceMemberRepository = workspaceMemberRepository;
    }


    public IReadOnlyCollection<UserProfile> Execute()
    {
        return _userRepository.GetAll().Select(user => new UserProfile
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role?.Name ?? _roleCatalog.ResolveName(user.RoleId),
            WorkspaceId = _workspaceMemberRepository.GetDefaultMembershipForUser(user.Id)?.WorkspaceId ?? Guid.Empty,
            IsActive = user.IsActive

        })
        .ToList();
    }
}
