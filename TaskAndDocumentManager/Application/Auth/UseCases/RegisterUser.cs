using System;
using Microsoft.Extensions.Logging;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Application.Common.Interfaces;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases
{
    public class RegisterUser
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailValidator _emailValidator;
        private readonly IPasswordValidator _passwordValidator;
        private readonly IRoleCatalog _roleCatalog;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
        private readonly ILogger<RegisterUser> _logger;
        private readonly IApplicationMetrics _metrics;

        public RegisterUser(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IEmailValidator emailValidator,
            IPasswordValidator passwordValidator,
            IRoleCatalog roleCatalog,
            IWorkspaceRepository workspaceRepository,
            IWorkspaceMemberRepository workspaceMemberRepository,
            ILogger<RegisterUser> logger,
            IApplicationMetrics metrics)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailValidator = emailValidator;
            _passwordValidator = passwordValidator;
            _roleCatalog = roleCatalog;
            _workspaceRepository = workspaceRepository;
            _workspaceMemberRepository = workspaceMemberRepository;
            _logger = logger;
            _metrics = metrics;
        }

        public void Execute(string email, string password)
        {
            if (!_emailValidator.IsValidEmail(email))
            {
                throw new FormatException("Email is invalid");
            }

            if (!_passwordValidator.IsPasswordStrong(password))
            {
                throw new ArgumentException("Password is not strong enough");
            }

            var existingUser = _userRepository.GetByEmail(email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User already exists");
            }

            var passwordHash = _passwordHasher.HashPassword(password);
            var userId = Guid.NewGuid();
            var workspace = new Workspace(BuildDefaultWorkspaceName(email), userId);

            var user = new User
            {
                Id = userId,
                Email = email,
                PasswordHash = passwordHash,
                RoleId = _roleCatalog.UserRoleId,
                IsActive = true
            };

            _userRepository.Save(user);
            _workspaceRepository.Add(workspace);
            _workspaceMemberRepository.Add(new WorkspaceMember(workspace.Id, user.Id, WorkspaceRoles.Owner));

            _logger.LogInformation(
                "User registered with user {UserId} and workspace {WorkspaceId}.",
                user.Id,
                workspace.Id);
            _metrics.RecordUserRegistered(user.Id, workspace.Id);
        }

        private static string BuildDefaultWorkspaceName(string email)
        {
            var normalizedEmail = email.Trim();
            var atIndex = normalizedEmail.IndexOf('@');
            var ownerName = atIndex > 0 ? normalizedEmail[..atIndex] : normalizedEmail;

            return $"{ownerName}'s Workspace";
        }
    }
}
