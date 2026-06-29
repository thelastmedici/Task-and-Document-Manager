namespace TaskAndDocumentManager.Application.Notifications.DTOs;

public sealed record NotificationQuery(
    int PageNumber = 1,
    int PageSize = 20)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 200;
}
