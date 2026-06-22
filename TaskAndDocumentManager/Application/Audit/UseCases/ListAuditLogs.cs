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
        AuditQuery query,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("Workspace ID is required.", nameof(workspaceId));
        }

        var normalizedQuery = NormalizeQuery(query) with { WorkspaceId = workspaceId };
        var auditLogs = await _auditLogRepository.SearchAuditLogsAsync(normalizedQuery, cancellationToken);

        var items = auditLogs.Items
            .Select(ToDto)
            .ToList();

        return new PaginatedResult<AuditLogDto>(
            items,
            auditLogs.TotalCount,
            auditLogs.Page,
            auditLogs.PageSize);
    }

    private static AuditQuery NormalizeQuery(AuditQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.UserId == Guid.Empty)
        {
            throw new ArgumentException("User ID filter cannot be empty.", nameof(query));
        }

        if (query.TimestampFromUtc.HasValue && query.TimestampToUtc.HasValue &&
            query.TimestampFromUtc.Value > query.TimestampToUtc.Value)
        {
            throw new ArgumentException(
                "Timestamp from date cannot be later than timestamp to date.",
                nameof(query));
        }

        var action = string.IsNullOrWhiteSpace(query.Action)
            ? null
            : query.Action.Trim();

        if (action is not null && !AuditActions.IsValid(action))
        {
            throw new ArgumentException("Action filter must be a supported audit action.", nameof(query));
        }

        return query with
        {
            PageNumber = query.PageNumber < 1 ? 1 : query.PageNumber,
            PageSize = query.PageSize < 1
                ? AuditQuery.DefaultPageSize
                : Math.Min(query.PageSize, AuditQuery.MaxPageSize),
            Action = action
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
