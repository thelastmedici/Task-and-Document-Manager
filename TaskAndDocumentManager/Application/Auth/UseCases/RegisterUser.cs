using System;
using System.Net.Mail;
using System.Text.RegularExpressions;

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
                            var hasNumber = new Regex(@"[0-9]+");
                            var hasUpperChar = new Regex(@"[A-Z]+");
                            var hasMinimum8Chars = new Regex(@".{8,}");

                            return hasNumber.IsMatch(password) && hasUpperChar.IsMatch(password) && hasMinimum8Chars.IsMatch(password);
                    
                }
        }
        
    }
