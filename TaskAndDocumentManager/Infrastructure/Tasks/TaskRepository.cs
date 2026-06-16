using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using TaskAndDocumentManager.Application.Tasks.DTOs;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Infrastructure.Tasks;

public class TaskRepository(TaskDbContext dbContext) : ITaskRepository
{
    private readonly TaskDbContext _dbContext = dbContext;

    public async Task CreateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        await _dbContext.Tasks.AddAsync(task, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tasks
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tasks
            .AsNoTracking()
            .OrderByDescending(task => task.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> SearchTasksAsync(
        TaskQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1
            ? TaskQuery.DefaultPageSize
            : Math.Min(query.PageSize, TaskQuery.MaxPageSize);
        var tasks = ApplyFilters(_dbContext.Tasks.AsNoTracking(), query);

        tasks = ApplySort(tasks, query);

        return await tasks
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountTasksAsync(
        TaskQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await ApplyFilters(_dbContext.Tasks.AsNoTracking(), query)
            .CountAsync(cancellationToken);
    }

    private static IQueryable<TaskItem> ApplyFilters(IQueryable<TaskItem> tasks, TaskQuery query)
    {
        var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
            ? null
            : query.SearchTerm.Trim();

        if (query.IsCompleted.HasValue)
        {
            tasks = tasks.Where(task => task.IsCompleted == query.IsCompleted.Value);
        }

        if (query.AssignedToUserId.HasValue)
        {
            tasks = tasks.Where(task => task.AssignedToUserId == query.AssignedToUserId.Value);
        }

        if (query.Priority.HasValue)
        {
            tasks = tasks.Where(task => task.Priority == query.Priority.Value);
        }

        if (query.DueAfterUtc.HasValue)
        {
            tasks = tasks.Where(task => task.DueAtUtc >= query.DueAfterUtc.Value);
        }

        if (query.DueBeforeUtc.HasValue)
        {
            tasks = tasks.Where(task => task.DueAtUtc <= query.DueBeforeUtc.Value);
        }

        if (query.OwnerId.HasValue)
        {
            if (query.IncludeAssignedTasks)
            {
                tasks = tasks.Where(task =>
                    task.OwnerId == query.OwnerId.Value ||
                    task.AssignedToUserId == query.OwnerId.Value);
            }
            else
            {
                tasks = tasks.Where(task => task.OwnerId == query.OwnerId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            tasks = tasks.Where(task =>
                EF.Functions.ILike(task.Title, pattern) ||
                EF.Functions.ILike(task.Description, pattern));
        }

        return tasks;
    }

    private static IQueryable<TaskItem> ApplySort(IQueryable<TaskItem> tasks, TaskQuery query)
    {
        return (query.SortBy, query.SortDirection) switch
        {
            (TaskSortBy.DueAt, SortDirection.Ascending) => tasks
                .OrderBy(task => task.DueAtUtc == null)
                .ThenBy(task => task.DueAtUtc)
                .ThenByDescending(task => task.CreatedAt),
            (TaskSortBy.DueAt, SortDirection.Descending) => tasks
                .OrderBy(task => task.DueAtUtc == null)
                .ThenByDescending(task => task.DueAtUtc)
                .ThenByDescending(task => task.CreatedAt),
            (TaskSortBy.Priority, SortDirection.Ascending) => tasks
                .OrderBy(task => task.Priority)
                .ThenByDescending(task => task.CreatedAt),
            (TaskSortBy.Priority, SortDirection.Descending) => tasks
                .OrderByDescending(task => task.Priority)
                .ThenByDescending(task => task.CreatedAt),
            (TaskSortBy.CreatedAt, SortDirection.Ascending) => tasks.OrderBy(task => task.CreatedAt),
            _ => tasks.OrderByDescending(task => task.CreatedAt)
        };
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        _dbContext.Tasks.Update(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _dbContext.Tasks.FirstOrDefaultAsync(
            existingTask => existingTask.Id == id,
            cancellationToken);

        if (task is null)
        {
            return;
        }

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
