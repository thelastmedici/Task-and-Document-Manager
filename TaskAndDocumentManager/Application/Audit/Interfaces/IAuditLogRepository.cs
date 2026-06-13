using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Application.Audit.DTOs;
using TaskAndDocumentManager.Application.Common.DTOs;

namespace TaskAndDocumentManager.Application.Audit.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<PaginatedResult<AuditLog>> GetPageAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}
