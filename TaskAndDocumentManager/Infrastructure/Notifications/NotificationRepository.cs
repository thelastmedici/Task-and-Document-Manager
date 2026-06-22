using Microsoft.EntityFrameworkCore;
using TaskAndDocumentManager.Application.Notifications.Interfaces;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Infrastructure.Tasks;

namespace TaskAndDocumentManager.Infrastructure.Notifications;

public class NotificationRepository(TaskDbContext dbContext) : INotificationRepository
{
    private readonly TaskDbContext _dbContext = dbContext;

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(
        Guid id,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications.FirstOrDefaultAsync(
            notification => notification.Id == id && notification.WorkspaceId == workspaceId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Notification>> GetByUserIdAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _dbContext.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.UserId == userId &&
                notification.WorkspaceId == workspaceId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return notifications;
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _dbContext.Notifications.Update(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
