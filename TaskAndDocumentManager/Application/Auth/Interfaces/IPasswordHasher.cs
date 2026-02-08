//THis interface holds the contract for password hasing and verification
namespace TaskAndDocumentManager.Application.Auth.Interfaces
{
    public interface IPasswordHasher
    {
         string Hash(string password);
         bool VerifyPassword(string password, string passwordHash);
    }
}