using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Notifications.DTOs;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Notifications.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken = default);
    Task<PaginatedResult<Notification>> SearchByUserIdAsync(
        Guid userId,
        Guid workspaceId,
        NotificationQuery query,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
}
