using System;
using System.Linq;
using System.Net.Mail;

namespace TaskAndDocumentManager.Application.Auth.UseCases
    {
        public class RegisterUser
        {

            //Implementation of register user usecases

            //1)Email validation
            

//                 Later:

// Extract IEmailValidator

// Move implementation out

// Plug it back in

        
                //TODO Password validation c#
                public bool IsPasswordStrong(string password)
                {    
                            if (string.IsNullOrWhiteSpace(password))
                            {
                                return false;
                            }
                            return password.Length >= 8 && password.Any(char.IsDigit) && password.Any(char.IsUpper);
                    
                }
        }
        
    }
