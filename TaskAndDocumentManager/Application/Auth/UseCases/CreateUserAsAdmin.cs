using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class CreateUserAsAdmin
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailValidator _emailValidator;
    private readonly IPasswordValidator _passwordValidator;
    private readonly IRoleCatalog _roleCatalog;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public CreateUserAsAdmin(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IEmailValidator emailValidator,
        IPasswordValidator passwordValidator,
        IRoleCatalog roleCatalog,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _emailValidator = emailValidator;
        _passwordValidator = passwordValidator;
        _roleCatalog = roleCatalog;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public void Execute(string email, string password, Guid roleId, Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        if (!_emailValidator.IsValidEmail(email))
        {
            throw new FormatException("Email is invalid.");
        }

        if (!_passwordValidator.IsPasswordStrong(password))
        {
            throw new ArgumentException("Password is not strong enough.");
        }

        if (!_roleCatalog.IsSupportedRole(roleId))
        {
            throw new ArgumentException("Role is invalid.");
        }

        var existingUser = _userRepository.GetByEmail(email);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("User already exists.");
        }

        var passwordHash = _passwordHasher.HashPassword(password);
        var userId = Guid.NewGuid();

        var user = new User
        {
            Id = userId,
            Email = email,
            PasswordHash = passwordHash,
            RoleId = roleId,
            IsActive = true
        };

        var savedUser = _userRepository.Save(user);
        _workspaceMemberRepository.Add(new WorkspaceMember(workspaceId, savedUser.Id, WorkspaceRoles.Member));
    }
}
