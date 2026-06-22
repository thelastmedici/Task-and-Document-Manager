using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Notifications.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Notification>> GetByUserIdAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
}
