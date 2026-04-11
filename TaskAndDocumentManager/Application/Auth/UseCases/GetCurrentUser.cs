using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class GetCurrentUser
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleCatalog _roleCatalog;

    public GetCurrentUser(IUserRepository userRepository, IRoleCatalog roleCatalog)
    {
        _userRepository = userRepository;
        _roleCatalog = roleCatalog;
    }

    public UserProfile Execute(int userId)
    {
        var user = _userRepository.GetById(userId);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return new UserProfile
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role?.Name ?? _roleCatalog.ResolveName(user.RoleId),
            IsActive = user.IsActive
        };
    }
}
