using TaskAndDocumentManager.Application.Workspaces.Interfaces;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Infrastructure.Workspaces;

public class InMemoryWorkspaceRepository : IWorkspaceRepository
{
    private static readonly List<Workspace> Workspaces = new();

    public Workspace Add(Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        if (Workspaces.All(existingWorkspace => existingWorkspace.Id != workspace.Id))
        {
            Workspaces.Add(workspace);
        }

        return workspace;
    }

    public Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default)
    {
        return Task.FromResult(Add(workspace));
    }

    public Workspace? GetById(Guid id)
    {
        return Workspaces.FirstOrDefault(workspace => workspace.Id == id);
    }

    public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(GetById(id));
    }
}
