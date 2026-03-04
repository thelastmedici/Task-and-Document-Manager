using System;
using System.Linq;
using System.Net.Mail;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Auth.UseCases
{
    public class RegisterUser
    {
         
          //A constructor that accept IUserRepository IPasswordHasher IEmailValidator and assigns them to the private field
         public RegisterUser(IUserRepository userRepository, IPasswordHasher passwordHasher, IEmailValidator emailValidator, IPasswordValidator passwordValidator){
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailValidator = emailValidator;
            _passwordValidator = passwordValidator;
        }

        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailValidator _emailValidator;

        private readonly IPasswordValidator _passwordValidator;


    
        public void Execute(string email, string password)
        {
            if(!_emailValidator.IsValidEmail(email))
            {
                throw new FormatException("Email is invalid");
            }

            //validate password strength
            if (!_passwordValidator.IsPasswordStrong(password))
            {
                throw new ArgumentException("Password Is not strong enough");
            }
            

            //checkif user exist
            var existingUser = _userRepository.GetByEmail(email);

            if(existingUser != null)
            {
                throw new InvalidOperationException("User Already Exist");
            }

            //hashpassword
            var hashPassword = _passwordHasher.HashPassword(password);

            //create new user
            var user = new User
            {
                Email = email,
                PasswordHash = hashPassword,
            };


            //save user
            _userRepository.Save(user);
        }
       
       

    }
}
