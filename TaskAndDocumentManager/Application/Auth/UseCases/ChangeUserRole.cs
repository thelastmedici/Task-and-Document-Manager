using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class ChangeUserRole
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleCatalog _roleCatalog;

    public ChangeUserRole(IUserRepository userRepository, IRoleCatalog roleCatalog)
    {
        _userRepository = userRepository;
        _roleCatalog = roleCatalog;
    }

    public void Execute(Guid userId, Guid roleId)
    {
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
    }
}
