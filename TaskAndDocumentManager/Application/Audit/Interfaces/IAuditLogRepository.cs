using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Audit.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default);
}
