using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Notifications.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchCreatedAsync(Notification notification, CancellationToken cancellationToken = default);
}
