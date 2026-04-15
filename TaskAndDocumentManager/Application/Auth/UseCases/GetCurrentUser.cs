using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class GetCurrentUser
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUser(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public UserProfile Execute(Guid userId)
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
            Role = user.Role?.Name ?? BuiltInRoles.ResolveName(user.RoleId),
            IsActive = user.IsActive
        };
    }
}
