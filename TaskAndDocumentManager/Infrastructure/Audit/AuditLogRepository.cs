using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Audit.DTOs;
using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Infrastructure.Audit;

public class AuditLogRepository : IAuditLogRepository
{
    private static readonly List<AuditLog> AuditLogs = new();

    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditLog);
        AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    public Task<PaginatedResult<AuditLog>> GetPageAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1
            ? AuditLogQuery.DefaultPageSize
            : Math.Min(query.PageSize, AuditLogQuery.MaxPageSize);

        var filteredLogs = AuditLogs.AsEnumerable();

        if (query.UserId.HasValue)
        {
            filteredLogs = filteredLogs.Where(auditLog => auditLog.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            filteredLogs = filteredLogs.Where(auditLog =>
                string.Equals(auditLog.Action, query.Action, StringComparison.Ordinal));
        }

        if (query.TimestampFromUtc.HasValue)
        {
            filteredLogs = filteredLogs.Where(auditLog =>
                auditLog.TimestampUtc >= query.TimestampFromUtc.Value);
        }

        if (query.TimestampToUtc.HasValue)
        {
            filteredLogs = filteredLogs.Where(auditLog =>
                auditLog.TimestampUtc <= query.TimestampToUtc.Value);
        }

        var orderedLogs = filteredLogs
            .OrderByDescending(auditLog => auditLog.TimestampUtc)
            .ToList();

        var items = orderedLogs
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PaginatedResult<AuditLog>(
            items,
            orderedLogs.Count,
            pageNumber,
            pageSize));
    }
}
