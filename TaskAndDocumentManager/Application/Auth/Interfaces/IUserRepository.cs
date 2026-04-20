using TaskAndDocumentManager.Domain.Auth;

namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface IUserRepository
{
    User Save(User user);
    User? GetById(Guid id);
    User? GetByEmail(string email);
    IReadOnlyCollection<User> GetAll();

    void Delete(Guid id);
}
