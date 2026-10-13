using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Workspaces.Interfaces;

public interface IWorkspaceRepository
{
    Workspace Add(Workspace workspace);
    Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default);
    Workspace? GetById(Guid id);
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default);
}