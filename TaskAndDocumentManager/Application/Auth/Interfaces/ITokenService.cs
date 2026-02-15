using System;

namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface ITokenService
{
    string GenerateToken(string userId, string email, string role);
    bool ValidateToken(string token);
    string GetUserIdFromToken(string token);
}

using System;

namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface ITokenService
{
    string GenerateToken(string userId, string email, string role);
    bool ValidateToken(string token);
    string GetUserIdFromToken(string token);
}

using System;

namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface ITokenService
{
    string GenerateToken(string userId, string email, string role);
    bool ValidateToken(string token);
    string GetUserIdFromToken(string token);
}