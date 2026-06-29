using Microsoft.EntityFrameworkCore;
using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Notifications.DTOs;
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

    public async Task<PaginatedResult<Notification>> SearchByUserIdAsync(
        Guid userId,
        Guid workspaceId,
        NotificationQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1
            ? NotificationQuery.DefaultPageSize
            : Math.Min(query.PageSize, NotificationQuery.MaxPageSize);

        var filteredNotifications = _dbContext.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.UserId == userId &&
                notification.WorkspaceId == workspaceId);

        var totalCount = await filteredNotifications.CountAsync(cancellationToken);

        var notifications = await filteredNotifications
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<Notification>(
            notifications,
            totalCount,
            pageNumber,
            pageSize);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _dbContext.Notifications.Update(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
