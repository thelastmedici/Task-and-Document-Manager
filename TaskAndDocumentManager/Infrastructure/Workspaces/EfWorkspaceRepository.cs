using Microsoft.EntityFrameworkCore;
using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Workspaces;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Infrastructure.Workspaces;

public class EfWorkspaceRepository(AppDbContext db) : IWorkspaceRepository
{
    public Workspace Add(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        db.Workspaces.Add(workspace);
        db.SaveChanges();
        return workspace;
    }

    public async Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(ct);
        return workspace;
    }

    public Workspace? GetById(Guid id)
    {
        return db.Workspaces
            .AsNoTracking()
            .FirstOrDefault(workspace => workspace.Id == id);
    }

    public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.Id == id, ct);
    }
}
