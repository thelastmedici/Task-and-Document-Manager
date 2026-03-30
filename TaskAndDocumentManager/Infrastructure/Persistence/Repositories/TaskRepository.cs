using Microsoft.EntityFrameworkCore;
using TaskAndDocumentManager.Application.Tasks.Dtos;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Infrastructure.Persistence.Repositories;

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

    public async Task<IReadOnlyList<TaskItem>> SearchAsync(
        ListTasksQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1
            ? ListTasksQuery.DefaultPageSize
            : Math.Min(query.PageSize, ListTasksQuery.MaxPageSize);
        var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
            ? null
            : query.SearchTerm.Trim();

        IQueryable<TaskItem> tasks = _dbContext.Tasks.AsNoTracking();

        if (query.IsCompleted.HasValue)
        {
            tasks = tasks.Where(task => task.IsCompleted == query.IsCompleted.Value);
        }

        if (query.AssignedToUserId.HasValue)
        {
            tasks = tasks.Where(task => task.AssignedToUserId == query.AssignedToUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            tasks = tasks.Where(task =>
                EF.Functions.Like(task.Title, pattern) ||
                EF.Functions.Like(task.Description, pattern));
        }

        return await tasks
            .OrderByDescending(task => task.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
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
