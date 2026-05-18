using TaskAndDocumentManager.Application.Audit.Interfaces;
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

    public Task<IReadOnlyCollection<AuditLog>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((IReadOnlyCollection<AuditLog>)AuditLogs.ToList());
    }
}
