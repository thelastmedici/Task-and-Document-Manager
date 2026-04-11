using TaskAndDocumentManager.Application.Auth.Interfaces;

namespace TaskAndDocumentManager.Infrastructure.Persistence;

public class RoleCatalog : IRoleCatalog
{
    public Guid AdminRoleId => BuiltInRoles.AdminId;

    public Guid ManagerRoleId => BuiltInRoles.ManagerId;

    public Guid UserRoleId => BuiltInRoles.UserId;

    public string ResolveName(Guid roleId)
    {
        return BuiltInRoles.ResolveName(roleId);
    }
}
