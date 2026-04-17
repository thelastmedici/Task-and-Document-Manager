using System.Security.Claims;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetActorId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(value, out var actorId))
        {
            throw new UnauthorizedAccessException("Invalid user identifier claim.");
        }

        return actorId;
    }

    public static string GetRoleName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new UnauthorizedAccessException("Missing role claim.");
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return string.Equals(
            principal.GetRoleName(),
            BuiltInRoles.AdminName,
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsManager(this ClaimsPrincipal principal)
    {
        return string.Equals(
            principal.GetRoleName(),
            BuiltInRoles.ManagerName,
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsUser(this ClaimsPrincipal principal)
    {
        return string.Equals(
            principal.GetRoleName(),
            BuiltInRoles.UserName,
            StringComparison.OrdinalIgnoreCase);
    }
}
