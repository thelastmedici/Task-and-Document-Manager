namespace TaskAndDocumentManager.Application.Notifications.DTOs;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAtUtc);
