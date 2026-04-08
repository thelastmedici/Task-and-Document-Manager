namespace TaskAndDocumentManager.Domain.Entities;

public static class BuiltInRoles
{
    public static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AdminId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid ManagerId = Guid.Parse("333333333-3333-3333-3333-333333333333");

    public const string UserName = "User";
    public const string AdminName = "Admin";

    public const string ManagerName = "Manager";

    public static Role CreateUserRole()
    {
        return new Role
        {
            Id = UserId,
            Name = UserName
        };
    }

    public static Role CreateAdminRole()
    {
        return new Role
        {
            Id = AdminId,
            Name = AdminName
        };
    }

    public static string ResolveName(Guid roleId)
    {
        return roleId == AdminId ? AdminName : UserName;
    }
}
