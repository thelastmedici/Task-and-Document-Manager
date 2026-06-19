using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Application.Workspaces.Interfaces;

public interface IWorkspaceRepository
{
    Workspace Add(Workspace workspace);
    Workspace? GetById(Guid id);
}
