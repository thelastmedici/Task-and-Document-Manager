using System;
using System.Linq;
using System.Net.Mail;
using TaskAndDocumentManager.Application.Auth.Interfaces;

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

            var existingUser = _userRepository.GetByEmail(email);

            if(existingUser != null)
            {
                throw new InvalidOperationException("User Already Exist");
            }
        }
       
       

    }
}
