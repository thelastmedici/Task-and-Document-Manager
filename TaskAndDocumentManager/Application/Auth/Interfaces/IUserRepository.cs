public interface IUserRepository
{
    User Save(User user);              // create OR update user
    User GetById(int id);              // fetch by primary key
    User GetByEmail(string email);     // fetch by unique email
}
