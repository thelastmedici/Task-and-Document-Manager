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

    public Workspace? GetById(Guid id)
    {
        return Workspaces.FirstOrDefault(workspace => workspace.Id == id);
    }
}
