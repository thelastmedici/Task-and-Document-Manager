namespace TaskAndDocumentManager.Api.Realtime;

public static class RealtimeEventNames
{
    public const string DocumentShared = nameof(DocumentShared);
    public const string DocumentDeleted = nameof(DocumentDeleted);
    public const string TaskAssigned = nameof(TaskAssigned);
    public const string TaskCompleted = nameof(TaskCompleted);
    public const string NotificationCreated = nameof(NotificationCreated);
    public const string UserOnline = nameof(UserOnline);
    public const string UserOffline = nameof(UserOffline);
    public const string UserPresenceUpdated = nameof(UserPresenceUpdated);
}
