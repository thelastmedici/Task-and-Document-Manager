namespace TaskAndDocumentManager.Domain.Entities;

public static class AuditActions
{
    public const string DocumentUploaded = nameof(DocumentUploaded);
    public const string DocumentDeleted = nameof(DocumentDeleted);
    public const string DocumentShared = nameof(DocumentShared);
    public const string DocumentAccessRevoked = nameof(DocumentAccessRevoked);
    public const string TaskCreated = nameof(TaskCreated);
    public const string TaskCompleted = nameof(TaskCompleted);
    public const string UserRoleChanged = nameof(UserRoleChanged);

    private static readonly HashSet<string> AllowedValues = new(StringComparer.Ordinal)
    {
        DocumentUploaded,
        DocumentDeleted,
        DocumentShared,
        DocumentAccessRevoked,
        TaskCreated,
        TaskCompleted,
        UserRoleChanged
    };

    public static bool IsValid(string action)
    {
        return !string.IsNullOrWhiteSpace(action) && AllowedValues.Contains(action.Trim());
    }
}
