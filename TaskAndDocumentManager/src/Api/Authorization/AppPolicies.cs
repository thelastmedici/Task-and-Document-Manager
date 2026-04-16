namespace TaskAndDocumentManager.Api.Authorization;

public static class AppPolicies
{
    public const string Authenticated = "Authenticated";
    public const string AdminOnly = "AdminOnly";

    public const string ManagerOrAdmin = "ManagerOrAdmin";
}