using System;
using System.Linq;
using System.Net.Mail;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Application.Auth.UseCases
{
    public class RegisterUser
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailValidator _emailValidator;


        
        //A constructor that accept IUserRepository IPasswordHasher IEmailValidator and assigns them to the private field
        public RegisterUser(IUserRepository userRepository, IPasswordHasher passwordHasher, IEmailValidator emailValidator){
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailValidator = emailValidator;
        }

    }
}
