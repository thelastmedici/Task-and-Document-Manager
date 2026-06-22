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
        Guid workspaceId,
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

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        var notification = await _notificationRepository.GetByIdAsync(notificationId, workspaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Notification not found.");

        if (notification.UserId != requestedByUserId)
        {
            throw new UnauthorizedAccessException("You can only update your own notifications.");
        }

        notification.MarkAsRead();
        await _notificationRepository.UpdateAsync(notification, cancellationToken);
    }
}
