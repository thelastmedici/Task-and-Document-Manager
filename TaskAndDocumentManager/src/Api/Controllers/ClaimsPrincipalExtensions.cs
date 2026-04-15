using System.Security.Claims;

namespace TaskAndDocumentManager.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
       public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if(!Guid.TryParse(value,  out var userId))
            {
                throw new UnauthorizedAccessException("INvalid user ID claim");
            }

            return userId;
        }
    }
}