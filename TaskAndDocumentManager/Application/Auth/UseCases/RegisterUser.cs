using System;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Application.Auth.UseCases
{
    public class RegisterUser
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailValidator _emailValidator;
        private readonly IPasswordValidator _passwordValidator;

        public RegisterUser(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IEmailValidator emailValidator,
            IPasswordValidator passwordValidator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailValidator = emailValidator;
            _passwordValidator = passwordValidator;
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

            var user = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                RoleId = BuiltInRoles.UserId,
                IsActive = true
            };

            _userRepository.Save(user);
        }
    }
}
