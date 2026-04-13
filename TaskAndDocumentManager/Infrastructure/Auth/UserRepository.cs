using System;
using System.Collections.Generic;
using System.Linq;
using TaskAndDocumentManager.Application.Auth.Interfaces;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Infrastructure.Auth
{
    public class UserRepository : IUserRepository
    {
        private static readonly List<User> _users = new();

        public User Save(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            AttachRoleIfMissing(user);

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

        public User? GetById(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return user is null ? null : CloneWithRole(user);
        }

        public User? GetByEmail(string email)
        {
            if (email == null) return null;

            var user = _users.FirstOrDefault(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

            return user is null ? null : CloneWithRole(user);
        }

        private static void AttachRoleIfMissing(User user)
        {
            if (user.Role != null)
            {
                return;
            }

            user.Role = new Role
            {
                Id = user.RoleId,
                Name = BuiltInRoles.ResolveName(user.RoleId)
            };
        }

        private static User CloneWithRole(User user)
        {
            return new User
            {
                Id = user.Id,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                RoleId = user.RoleId,
                Role = new Role
                {
                    Id = user.Role?.Id ?? user.RoleId,
                    Name = user.Role?.Name ?? BuiltInRoles.ResolveName(user.RoleId)
                },
                IsActive = user.IsActive
            };
        }
    }
}
