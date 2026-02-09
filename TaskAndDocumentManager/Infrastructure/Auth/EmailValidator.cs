// Email validation authentication
using System.Net.Mail;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Auth;

public class EmailValidator : IEmailValidator
{
    public bool IsValidEmail(string email)
    {
        try
        {
            var _ = new MailAddress(email); // the "_" is the discard variable used in c# for a variable that is not needed
            return true;
        }
        catch(FormatException)
        {
            return false;
        }
    }
}
