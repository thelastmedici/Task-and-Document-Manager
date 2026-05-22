using TaskAndDocumentManager.Application.Notifications.Interfaces;

namespace TaskAndDocumentManager.Application.Notifications.UseCases;

public class MarkNotificationAsRead
{
    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationAsRead(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task ExecuteAsync(
        Guid notificationId,
        Guid requestedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty)
        {
            throw new ArgumentException("Notification ID is required.", nameof(notificationId));
        }

        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.", nameof(requestedByUserId));
        }

        var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken)
            ?? throw new KeyNotFoundException("Notification not found.");

        if (notification.UserId != requestedByUserId)
        {
            throw new UnauthorizedAccessException("You can only update your own notifications.");
        }

        notification.MarkAsRead();
        await _notificationRepository.UpdateAsync(notification, cancellationToken);
    }
}
