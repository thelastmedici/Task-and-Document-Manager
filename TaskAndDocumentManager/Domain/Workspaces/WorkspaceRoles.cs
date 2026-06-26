namespace TaskAndDocumentManager.Domain.Workspaces;

public static class WorkspaceRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Member = "Member";

    public static readonly IReadOnlyCollection<string> All =
    [
        Owner,
        Admin,
        Manager,
        Member
    ];

    public static bool IsSupported(string role)
    {
        return !string.IsNullOrWhiteSpace(role) &&
            All.Any(supportedRole => string.Equals(
                supportedRole,
                role.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    public static string Normalize(string role)
    {
        if (!IsSupported(role))
        {
            throw new ArgumentException("Workspace role is invalid.", nameof(role));
        }

        return All.First(supportedRole => string.Equals(
            supportedRole,
            role.Trim(),
            StringComparison.OrdinalIgnoreCase));
    }
}
