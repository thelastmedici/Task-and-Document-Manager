namespace TaskAndDocumentManager.Application.Auth.Interfaces;

public interface IRoleCatalog
{
    Guid AdminRoleId { get; }
    Guid ManagerRoleId { get; }
    Guid UserRoleId { get; }

    string ResolveName(Guid roleId);
}
