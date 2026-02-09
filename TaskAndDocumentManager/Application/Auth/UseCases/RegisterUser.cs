using System;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace TaskAndDocumentManager.Application.Auth.UseCases
    {
        public class RegisterUser
        {

            //Implementation of register user usecases

            //1)Email validation
            private bool IsValidEmail(string email)
            {
                var trimmedEmail = email.Trim();

                if(trimmedEmail.EndsWith("."))
                {
                    return false;
                }
                try
                {
                    var addr = new MailAddress(email);
                }
                catch
                {
                    return false;
                }
                return true;

//                 Later:

// Extract IEmailValidator

// Move implementation out

// Plug it back in
            }

        
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

                // public bool VerifyPassword(string inputPassword, string storedHash)
                // {
                //     var parts = storedHash.Split('.');
                //     if (parts.Length != 2)
                //     {
                //         return false;
                //     }

                //     var salt = Convert.FromBase64String(parts[0]);
                //     var hash = Convert.FromBase64String(parts[1]);

                //     var inputHash = Rfc2898DeriveBytes.Pbkdf2(inputPassword, salt, 100000, HashAlgorithmName.SHA256, 32);

                //     return CryptographicOperations.FixedTimeEquals(hash, inputHash);
                // }
        }
        
    }
