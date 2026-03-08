using System;
using TaskAndDocumentManager.Application.Auth.DTOs;

namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface ITokenService
{
    TokenResult GenerateToken(string userId, string email, string role);
    bool ValidateToken(string token);
    string GetUserIdFromToken(string token);
}
