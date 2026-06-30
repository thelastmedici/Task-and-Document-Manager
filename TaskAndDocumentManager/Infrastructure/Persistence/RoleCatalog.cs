using Microsoft.Extensions.Caching.Memory;
using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Persistence;

public class RoleCatalog : IRoleCatalog
{
    private const string CacheKey = "auth.role-catalog.v1";

    private readonly IMemoryCache _cache;

    public RoleCatalog(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Guid AdminRoleId => GetCatalog().AdminRoleId;

    public Guid ManagerRoleId => GetCatalog().ManagerRoleId;

    public Guid UserRoleId => GetCatalog().UserRoleId;

    public string ResolveName(Guid roleId)
    {
        var catalog = GetCatalog();
        return catalog.RoleNames.TryGetValue(roleId, out var roleName)
            ? roleName
            : BuiltInRoles.UserName;
    }

    public bool IsSupportedRole(Guid roleId)
    {
        return GetCatalog().RoleNames.ContainsKey(roleId);
    }

    private RoleCatalogSnapshot GetCatalog()
    {
        return _cache.GetOrCreate(
            CacheKey,
            entry =>
            {
                entry.Priority = CacheItemPriority.NeverRemove;

                var roleNames = new Dictionary<Guid, string>
                {
                    [BuiltInRoles.AdminId] = BuiltInRoles.AdminName,
                    [BuiltInRoles.ManagerId] = BuiltInRoles.ManagerName,
                    [BuiltInRoles.UserId] = BuiltInRoles.UserName
                };

                return new RoleCatalogSnapshot(
                    BuiltInRoles.AdminId,
                    BuiltInRoles.ManagerId,
                    BuiltInRoles.UserId,
                    roleNames);
            })!;
    }

    private sealed record RoleCatalogSnapshot(
        Guid AdminRoleId,
        Guid ManagerRoleId,
        Guid UserRoleId,
        IReadOnlyDictionary<Guid, string> RoleNames);
}
