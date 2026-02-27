namespace TaskAndDocumentManager.Application.Auth.Interfaces
{
    public interface IPasswordValidator
    {
        public bool IsPasswordStrong(string password);
    }
}