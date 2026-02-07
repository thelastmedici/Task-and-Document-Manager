//email validation contract interface for usecase lives here
namespace TaskAndDocumentManager.Apploication.Auth.interfaces
{
    public interface IEmailValidator
    {
        bool IsValid(string email);
    }
}