using TaskAndDocumentManager.Application.Common.DTOs;
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

    public async Task<PaginatedResult<NotificationDto>> ExecuteAsync(
        Guid userId,
        Guid workspaceId,
        NotificationQuery query,
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

        ArgumentNullException.ThrowIfNull(query);

        var notifications = await _notificationRepository.SearchByUserIdAsync(
            userId,
            workspaceId,
            query,
            cancellationToken);

        var items = notifications.Items
            .Select(notification => new NotificationDto(
                notification.Id,
                notification.Title,
                notification.Message,
                notification.IsRead,
                notification.CreatedAtUtc))
            .ToList();

        return new PaginatedResult<NotificationDto>(
            items,
            notifications.TotalCount,
            notifications.Page,
            notifications.PageSize);
    }
}
