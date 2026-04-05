using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Tasks.Interfaces;

namespace TaskAndDocumentManager.Application.Documents.UseCases;

public class LinkDocumentToTask
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ITaskRepository _taskRepository;

    public LinkDocumentToTask(
        IDocumentRepository documentRepository,
        ITaskRepository taskRepository)
    {
        _documentRepository = documentRepository;
        _taskRepository = taskRepository;
    }

    public async Task ExecuteAsync(
        LinkDocumentToTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken)
            ?? throw new FileNotFoundException("Document not found.");

        if (document.UploadedByUserId != request.RequestedByUserId)
        {
            throw new UnauthorizedAccessException("Only the owner can link this document to a task.");
        }

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);

        if (task is null)
        {
            throw new FileNotFoundException("Task not found.");
        }

        document.LinkToTask(request.TaskId);
        await _documentRepository.UpdateAsync(document, cancellationToken);
    }
}
