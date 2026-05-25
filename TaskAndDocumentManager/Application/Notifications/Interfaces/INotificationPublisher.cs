using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Notifications.Interfaces;

public interface INotificationPublisher
{
    Task PublishCreatedAsync(Notification notification, CancellationToken cancellationToken = default);
}
