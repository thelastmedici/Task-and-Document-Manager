//email validation  interface for usecase lives here i.e WHAT
namespace TaskAndDocumentManager.Application.Auth.Interfaces
{
    public interface IEmailValidator
    {
        bool IsValidEmail(string email);
    }
}