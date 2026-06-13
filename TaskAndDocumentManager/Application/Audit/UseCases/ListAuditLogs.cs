using TaskAndDocumentManager.Application.Audit.DTOs;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Audit.UseCases;

public class ListAuditLogs
{
    private readonly IAuditLogRepository _auditLogRepository;

    public ListAuditLogs(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<PaginatedResult<AuditLogDto>> ExecuteAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        var auditLogs = await _auditLogRepository.GetPageAsync(normalizedQuery, cancellationToken);

        var items = auditLogs.Items
            .Select(ToDto)
            .ToList();

        return new PaginatedResult<AuditLogDto>(
            items,
            auditLogs.TotalCount,
            auditLogs.Page,
            auditLogs.PageSize);
    }

    private static AuditLogQuery NormalizeQuery(AuditLogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return query with
        {
            PageNumber = query.PageNumber < 1 ? 1 : query.PageNumber,
            PageSize = query.PageSize < 1
                ? AuditLogQuery.DefaultPageSize
                : Math.Min(query.PageSize, AuditLogQuery.MaxPageSize)
        };
    }

    private static AuditLogDto ToDto(AuditLog auditLog)
    {
        return new AuditLogDto(
            auditLog.Id,
            auditLog.UserId,
            auditLog.Action,
            auditLog.EntityType,
            auditLog.EntityId,
            auditLog.TimestampUtc);
    }
}
