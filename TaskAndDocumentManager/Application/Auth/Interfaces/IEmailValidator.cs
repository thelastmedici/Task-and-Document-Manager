//email validation contract interface for usecase lives here
namespace TaskAndDocumentManager.Application.Auth.Interfaces
{
    public interface IEmailValidator
    {
        bool IsValid(string email);
    }
}