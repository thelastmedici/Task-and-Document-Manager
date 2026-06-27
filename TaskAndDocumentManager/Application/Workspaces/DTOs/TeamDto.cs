namespace TaskAndDocumentManager.Application.Workspaces.DTOs;

public sealed record TeamDto(
    Guid Id,
    Guid WorkspaceId,
    string Name);
