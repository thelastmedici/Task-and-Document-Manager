using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Auth.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private static readonly object HashUser = new();
        private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _passwordHasher;

        public PasswordHasher()
        {
            _passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<object>(
                Options.Create(new PasswordHasherOptions
                {
                    CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
                    IterationCount = 210_000
                }));
        }

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(HashUser, password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            if (IsLegacyHashFormat(passwordHash))
            {
                return VerifyLegacyPassword(password, passwordHash);
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(HashUser, passwordHash, password);

            return verificationResult is PasswordVerificationResult.Success or
                PasswordVerificationResult.SuccessRehashNeeded;
        }

        public bool NeedsRehash(string passwordHash)
        {
            return IsLegacyHashFormat(passwordHash);
        }

        private static bool IsLegacyHashFormat(string passwordHash)
        {
            return !string.IsNullOrWhiteSpace(passwordHash) && passwordHash.Contains('.');
        }

        private static bool VerifyLegacyPassword(string password, string passwordHash)
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            try
            {
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
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
