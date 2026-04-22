using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Auth;

namespace TaskAndDocumentManager.Application.Auth.UseCases;


public class ListUsers
{
    private readonly IUserRepository _userRepository;

    private readonly IRoleCatalog _roleCatalog;


    public ListUsers(IUserRepository userRepository, IRoleCatalog roleCatalog)
    {
        _userRepository = userRepository;
        _roleCatalog = roleCatalog;
    }


    public IReadOnlyCollection<UserProfile> Execute()
    {
        return _userRepository.GetAll().Select(user => new UserProfile
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role?.Name ?? _roleCatalog.ResolveName(user.RoleId),
            IsActive = user.IsActive

        })
        .ToList();
    }
}