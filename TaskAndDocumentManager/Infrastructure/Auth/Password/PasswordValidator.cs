using System.Linq;
using TaskAndDocumentManager.Application.Auth.Interfaces;
namespace TaskAndDocumentManager.Infrastructure.Auth.Services
{
    public class PasswordValidator : IPasswordValidator
    {
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