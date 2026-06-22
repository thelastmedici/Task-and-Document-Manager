using TaskAndDocumentManager.Application.Notifications.DTOs;
using TaskAndDocumentManager.Application.Notifications.Interfaces;

namespace TaskAndDocumentManager.Application.Notifications.UseCases;

public class GetNotifications
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotifications(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IReadOnlyCollection<NotificationDto>> ExecuteAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        var notifications = await _notificationRepository.GetByUserIdAsync(
            userId,
            workspaceId,
            cancellationToken);

        return notifications
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.Title,
                notification.Message,
                notification.IsRead,
                notification.CreatedAtUtc))
            .ToList();
    }
}
