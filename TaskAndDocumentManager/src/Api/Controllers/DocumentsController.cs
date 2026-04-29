using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;

namespace TaskAndDocumentManager.Controllers;

[Authorize(Policy = AppPolicies.Authenticated)]
[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly UploadDocument _uploadDocument;
    private readonly LinkDocumentToTask _linkDocumentToTask;
    private readonly ShareDocument _shareDocument;
    private readonly ShareTaskLinkedDocument _shareTaskLinkedDocument;
    private readonly DownloadDocument _downloadDocument;
    private readonly DeleteDocument _deleteDocument;
    private readonly GetDocumentMetadata _getDocumentMetadata;
    private readonly ListAccessibleDocuments _listAccessibleDocuments;

    public DocumentsController(
        IDocumentRepository documentRepository,
        IDocumentAccessRepository documentAccessRepository,
        ITaskRepository taskRepository,
        IFileStorageService fileStorageService,
        UploadDocument uploadDocument,
        LinkDocumentToTask linkDocumentToTask,
        ShareDocument shareDocument,
        ShareTaskLinkedDocument shareTaskLinkedDocument,
        DownloadDocument downloadDocument,
        DeleteDocument deleteDocument,
        GetDocumentMetadata getDocumentMetadata,
        ListAccessibleDocuments listAccessibleDocuments)
    {
        _documentRepository = documentRepository;
        _documentAccessRepository = documentAccessRepository;
        _taskRepository = taskRepository;
        _fileStorageService = fileStorageService;
        _uploadDocument = uploadDocument;
        _linkDocumentToTask = linkDocumentToTask;
        _shareDocument = shareDocument;
        _shareTaskLinkedDocument = shareTaskLinkedDocument;
        _downloadDocument = downloadDocument;
        _deleteDocument = deleteDocument;
        _getDocumentMetadata = getDocumentMetadata;
        _listAccessibleDocuments = listAccessibleDocuments;
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
// for upload
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentFormRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();

        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { message = "A non-empty file is required." });
        }

        try
        {
            await using var stream = request.File.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);

            var documentId = await _uploadDocument.ExecuteAsync(
                new UploadDocumentRequest
                {
                    FileName = request.File.FileName,
                    ContentType = string.IsNullOrWhiteSpace(request.File.ContentType)
                        ? "application/octet-stream"
                        : request.File.ContentType,
                    Content = memoryStream.ToArray(),
                    UploadedByUserId = actorId
                },
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new
            {
                id = documentId,
                message = "Document uploaded successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while uploading the document." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListAll(CancellationToken cancellationToken)
    {
        if (!User.IsAdmin())
        {
            var actorId = User.GetActorId();
            var accessibleDocuments = await _listAccessibleDocuments.ExecuteAsync(
                actorId,
                User.IsManager(),
                cancellationToken);
            return Ok(accessibleDocuments);
        }

        var documents = await _documentRepository.GetAllAsync(cancellationToken);

        var dtos = documents.Select(d => new DocumentMetadataDto(
            d.Id,
            d.FileName,
            d.ContentType,
            d.SizeInBytes,
            d.UploadedByUserId,
            d.UploadedAtUtc,
            d.LinkedTaskId)).ToList();

        return Ok(dtos);
    }

    [HttpPost("{id:guid}/link-task")]
    public async Task<IActionResult> LinkToTask(
        Guid id,
        [FromBody] LinkDocumentToTaskBody request,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();

        if (User.IsAdmin())
        {
            var adminDocument = await _documentRepository.GetByIdAsync(id, cancellationToken);
            if (adminDocument is null)
            {
                return NotFound(new { message = "Document not found" });
            }

            var adminTask = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
            if (adminTask is null)
            {
                return NotFound(new { message = "Task not found" });
            }

            adminDocument.LinkToTask(request.TaskId);
            await _documentRepository.UpdateAsync(adminDocument, cancellationToken);
            return NoContent();
        }

        try
        {
            await _linkDocumentToTask.ExecuteAsync(
                new LinkDocumentToTaskRequest
                {
                    DocumentId = id,
                    TaskId = request.TaskId,
                    RequestedByUserId = actorId
                },
                cancellationToken);

            return NoContent();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while linking the document to the task." });
        }
    }

    [HttpPost("{id:guid}/share")]
    public async Task<IActionResult> Share(
        Guid id,
        [FromBody] ShareDocumentBody request,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();

        if (User.IsAdmin())
        {
            var adminDocument = await _documentRepository.GetByIdAsync(id, cancellationToken);
            if (adminDocument is null)
            {
                return NotFound(new { message = "Document not found" });
            }

            await _documentAccessRepository.GrantAccessAsync(
                new Domain.Documents.DocumentAccess(id, request.TargetUserId, actorId),
                cancellationToken);

            return NoContent();
        }

        try
        {
            await _shareDocument.ExecuteAsync(
                new ShareDocumentRequest
                {
                    DocumentId = id,
                    TargetUserId = request.TargetUserId,
                    GrantedByUserId = actorId
                },
                cancellationToken);

            return NoContent();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while sharing the document." });
        }
    }


    [HttpPost("{id:guid}/tasks/{taskId:guid}/share")]
    public async Task<IActionResult> ShareForTask(
        Guid id,
        Guid taskId,
        [FromBody] ShareTaskLinkedDocumentBody request,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();

        if (User.IsAdmin())
        {
            var adminDocument = await _documentRepository.GetByIdAsync(id, cancellationToken);
            if (adminDocument is null)
            {
                return NotFound(new { message = "Document not found" });
            }

            if (adminDocument.LinkedTaskId is null)
            {
                return Conflict(new { message = "Document is not linked to a task." });
            }

            if (adminDocument.LinkedTaskId.Value != taskId)
            {
                return Conflict(new { message = "Document is linked to a different task." });
            }

            var adminTask = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
            if (adminTask is null)
            {
                return NotFound(new { message = "Task not found" });
            }

            await _documentAccessRepository.GrantAccessAsync(
                new Domain.Documents.DocumentAccess(id, request.TargetUserId, actorId),
                cancellationToken);

            return NoContent();
        }


        try
        {
            await _shareTaskLinkedDocument.ExecuteAsync(
                new ShareTaskLinkedDocumentRequest
                {
                    DocumentId = id,
                    TaskId = taskId,
                    TargetUserId = request.TargetUserId,
                    GrantedByUserId = actorId
                },
                cancellationToken);

            return NoContent();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while sharing the task-linked document." });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMetadata(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();

        if (User.IsAdmin())
        {
            var adminDocument = await _documentRepository.GetByIdAsync(id, cancellationToken);
            if (adminDocument is null)
            {
                return NotFound(new { message = "Document not found" });
            }

            return Ok(new DocumentMetadataDto(
                adminDocument.Id,
                adminDocument.FileName,
                adminDocument.ContentType,
                adminDocument.SizeInBytes,
                adminDocument.UploadedByUserId,
                adminDocument.UploadedAtUtc,
                adminDocument.LinkedTaskId));
        }

        try
        {
            var metadata = await _getDocumentMetadata.ExecuteAsync(
                id,
                actorId,
                User.IsManager(),
                cancellationToken);
            return Ok(metadata);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();

        if (User.IsAdmin())
        {
            var adminDocument = await _documentRepository.GetByIdAsync(id, cancellationToken);
            if (adminDocument is null)
            {
                return NotFound(new { message = "Document not found" });
            }

            var stream = await _fileStorageService.OpenReadAsync(adminDocument.StoragePath, cancellationToken);
            return File(stream, adminDocument.ContentType, adminDocument.FileName);
        }

        try
        {
            var metadata = await _getDocumentMetadata.ExecuteAsync(
                id,
                actorId,
                false,
                cancellationToken);

            return Ok(metadata);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();

        if (User.IsAdmin())
        {
            var adminDocument = await _documentRepository.GetByIdAsync(id, cancellationToken);
            if (adminDocument is null)
            {
                return NotFound(new { message = "Document not found" });
            }

            await _fileStorageService.DeleteAsync(adminDocument.StoragePath, cancellationToken);
            await _documentRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

        try
        {
            await _deleteDocument.ExecuteAsync(id, actorId, cancellationToken);
            return NoContent();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while deleting the document." });
        }
    }

    public sealed class UploadDocumentFormRequest
    {
        public IFormFile? File { get; init; }
    }

    public sealed class LinkDocumentToTaskBody
    {
        public Guid TaskId { get; init; }
    }

    public sealed class ShareDocumentBody
    {
        public Guid TargetUserId { get; init; }
    }

    public sealed class ShareTaskLinkedDocumentBody
    {
        public Guid TargetUserId { get; init; }
    }

    private bool CanManageOwnedDocument(Guid ownerId, Guid actorId)
    {
        if (User.IsAdmin())
        {
            return true;
        }

        return ownerId == actorId;
    }

    private bool CanManageTaskLinkedDocument(Domain.Documents.Document document, Domain.Tasks.TaskItem task, Guid actorId)
    {
        if (User.IsAdmin())
        {
            return true;
        }

        if (User.IsManager())
        {
            return document.UploadedByUserId == actorId &&
                   (task.OwnerId == actorId || task.AssignedToUserId == actorId);
        }

        return document.UploadedByUserId == actorId &&
               task.OwnerId == actorId;
    }
}
