using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Infrastructure.Notifications;

public class NotificationRepository : INotificationRepository
{
    private static readonly List<Notification> Notifications = new();

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        Notifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notifications = Notifications
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ToList();

        return Task.FromResult((IReadOnlyCollection<Notification>)notifications);
    }
}
