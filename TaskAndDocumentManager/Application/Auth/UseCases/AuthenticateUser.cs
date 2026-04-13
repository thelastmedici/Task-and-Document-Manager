using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Application.Auth.UseCases;

public class AuthenticateUser
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthenticateUser(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public AuthResponse Execute(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = _userRepository.GetByEmail(normalizedEmail);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("This account is deactivated.");
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var roleName = user.Role?.Name ?? BuiltInRoles.ResolveName(user.RoleId);

        var tokenResult = _tokenService.GenerateToken(user.Id.ToString(), user.Email, roleName);

        return new AuthResponse
        {
            Token = tokenResult.Token,
            ExpiresAtUtc = tokenResult.ExpiresAtUtc,
            User = new UserProfile
            {
                Id = user.Id,
                Email = user.Email,
                Role = roleName,
                IsActive = user.IsActive
            }
        };
    }
}
