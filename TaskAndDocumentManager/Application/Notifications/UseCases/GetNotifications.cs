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
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var notifications = await _notificationRepository.GetByUserIdAsync(userId, cancellationToken);

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
