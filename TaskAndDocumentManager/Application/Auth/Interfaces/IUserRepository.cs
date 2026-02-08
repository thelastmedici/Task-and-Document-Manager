using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface IUserRepository
{
    User Save(User user);
    User GetById(int id);
    User GetByEmail(string email);
}
