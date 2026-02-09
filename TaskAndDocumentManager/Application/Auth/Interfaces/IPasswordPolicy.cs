namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface IPasswordPolicy
{
    bool IsPasswordStrong(string password);
}