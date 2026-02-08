//THis interface holds the contract for password hasing and verification
namespace TaskAndDocumentManager.Application.Auth.interfaces
{
    public interface IPasswordHasher
    {
         string Hash(string password);
         bool VerifyPassword(string password);
    }
}