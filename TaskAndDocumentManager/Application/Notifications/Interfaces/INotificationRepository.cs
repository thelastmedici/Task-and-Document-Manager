using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Notifications.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Notification>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
