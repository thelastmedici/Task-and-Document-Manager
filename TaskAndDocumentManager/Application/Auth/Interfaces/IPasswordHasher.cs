//THis interface holds the contract for password hasing and verification.. APPLICATION LAYER KNOWS ****WHAT***
namespace TaskAndDocumentManager.Application.Auth.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
    }
}