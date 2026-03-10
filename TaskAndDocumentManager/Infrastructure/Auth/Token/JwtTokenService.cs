using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskAndDocumentManager.Application.Auth.DTOs;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Auth.Token;

public class JwtTokenService : ITokenService
{
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly byte[] _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiresMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var key = jwtSection["Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("JWT signing key is missing. Configure Jwt:Key.");
        }

        _key = Encoding.UTF8.GetBytes(key);
        _issuer = jwtSection["Issuer"] ?? "TaskAndDocumentManager";
        _audience = jwtSection["Audience"] ?? "TaskAndDocumentManager.Client";
        _expiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var minutes) ? minutes : 60;
    }

    public TokenResult GenerateToken(string userId, string email, string role)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_expiresMinutes);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var jwtToken = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var token = _tokenHandler.WriteToken(jwtToken);

        return new TokenResult
        {
            Token = token,
            ExpiresAtUtc = expires
        };
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            _tokenHandler.ValidateToken(token, BuildValidationParameters(), out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetUserIdFromToken(string token)
    {
        if (!ValidateToken(token))
        {
            throw new UnauthorizedAccessException("Invalid token.");
        }

        var jwtToken = _tokenHandler.ReadJwtToken(token);
        var userId = jwtToken.Claims.FirstOrDefault(claim =>
            claim.Type == JwtRegisteredClaimNames.Sub || claim.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("Token does not contain a user identifier.");
        }

        return userId;
    }

    private TokenValidationParameters BuildValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_key),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }
}
