// ...existing code...
using System;
using System.Collections.Generic;
using System.Linq;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Infrastructure.Persistence.Repositories
{
        // This is a dummy implementation for demonstration purposes.
    // You should replace this with your actual data access logic (e.g., using Entity Framework Core).
    public class UserRepository : IUserRepository
    {
       private static readonly List<User> _users = new();

       public User Save(User user)
       {
           if (user == null) throw new ArgumentNullException(nameof(user));

           if (user.Id == 0)
           {
               var nextId = _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1;
               user.Id = nextId;
               _users.Add(user);
               return user;
           }

           var existing = _users.FirstOrDefault(u => u.Id == user.Id);
           if (existing != null)
           {
               _users.Remove(existing);
               _users.Add(user);
               return user;
           }

           _users.Add(user);
           return user;
       }

       public User GetById(int id)
       {
           return _users.FirstOrDefault(u => u.Id == id);
       }

       public User GetByEmail(string email)
       {
           if (email == null) return null;
           return _users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
       }
    }    
}
// ...existing code...