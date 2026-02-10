using System;
using System.Security.Cryptography;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Auth.Services
{
    
    public class PasswordHasher : IPasswordHasher
    {

         //TODO Password validation c#
                public bool IsPasswordStrong(string password)
                {    
                            if (string.IsNullOrWhiteSpace(password))
                            {
                                return false;
                            }
                            return password.Length >= 8 && password.Any(char.IsDigit) && password.Any(char.IsUpper);
                    
                }
        public string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                32
            );

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var storedHash = Convert.FromBase64String(parts[1]);

            var inputHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                32
            );

            return CryptographicOperations.FixedTimeEquals(storedHash, inputHash);
        }
    }
}
