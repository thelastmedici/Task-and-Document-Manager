using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Api.Routing;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Controllers;

[Authorize(Policy = AppPolicies.Authenticated)]
[ApiController]
[Route(ApiRoutes.Documents)]
public class DocumentsController : ControllerBase
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessRepository _documentAccessRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly UploadDocument _uploadDocument;
    private readonly LinkDocumentToTask _linkDocumentToTask;
    private readonly ShareDocument _shareDocument;
    private readonly ShareTaskLinkedDocument _shareTaskLinkedDocument;
    private readonly RevokeDocumentAccess _revokeDocumentAccess;
    private readonly GetSharedDocuments _getSharedDocuments;
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
        RevokeDocumentAccess revokeDocumentAccess,
        GetSharedDocuments getSharedDocuments,
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
        _revokeDocumentAccess = revokeDocumentAccess;
        _getSharedDocuments = getSharedDocuments;
        _downloadDocument = downloadDocument;
        _deleteDocument = deleteDocument;
        _getDocumentMetadata = getDocumentMetadata;
        _listAccessibleDocuments = listAccessibleDocuments;
    }

    [HttpPost]
    [RequestSizeLimit(MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentFormRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { message = "A non-empty file is required." });
        }

        var actorId = User.GetActorId();
        var workspaceId = User.GetWorkspaceId();

        try
        {
            await using var stream = request.File.OpenReadStream();

            var result = await _uploadDocument.ExecuteAsync(
                new UploadDocumentRequest
                {
                    FileName = request.File.FileName,
                    ContentType = string.IsNullOrWhiteSpace(request.File.ContentType)
                        ? "application/octet-stream"
                        : request.File.ContentType,
                    Content = stream,
                    SizeInBytes = request.File.Length,
                    UploadedByUserId = actorId,
                    WorkspaceId = workspaceId
                },
                cancellationToken);

            return CreatedAtAction(
                nameof(GetMetadata),
                new { id = result.DocumentId },
                new UploadDocumentResponse(result.DocumentId, result.FileName));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "The document could not be uploaded. Please try again." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ListAll(
        [FromQuery] DocumentListRequest request,
        CancellationToken cancellationToken)
    {
        var workspaceId = User.GetWorkspaceId();
        var query = new DocumentQuery(
            request.SearchTerm,
            request.ContentType,
            request.UploadedFromUtc,
            request.UploadedToUtc,
            request.PageNumber,
            request.PageSize);

        try
        {
            if (!User.IsAdmin())
            {
                var actorId = User.GetActorId();
                var accessibleDocuments = await _listAccessibleDocuments.ExecuteAsync(
                    actorId,
                    workspaceId,
                    User.IsManager(),
                    query,
                    cancellationToken);
                return Ok(accessibleDocuments);
            }

            var documents = await _listAccessibleDocuments.ExecuteForAdminAsync(workspaceId, query, cancellationToken);
            return Ok(documents);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("shared-with-me")]
    public async Task<IActionResult> SharedWithMe(
        [FromQuery] DocumentListRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();
        var workspaceId = User.GetWorkspaceId();
        var query = new DocumentQuery(
            request.SearchTerm,
            request.ContentType,
            request.UploadedFromUtc,
            request.UploadedToUtc,
            request.PageNumber,
            request.PageSize);

        try
        {
            var documents = await _getSharedDocuments.ExecuteAsync(actorId, workspaceId, query, cancellationToken);
            return Ok(documents);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/link-task")]
    public async Task<IActionResult> LinkToTask(
        Guid id,
        [FromBody] LinkDocumentToTaskBody request,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();
        var workspaceId = User.GetWorkspaceId();

        if (User.IsAdmin())
        {
            var adminDocument = await _documentRepository.GetByIdInWorkspaceAsync(id, workspaceId, cancellationToken);
            if (adminDocument is null)
            {
                return NotFound(new { message = "Document not found" });
            }

            var adminTask = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
            if (adminTask is null || adminTask.WorkspaceId != workspaceId)
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
                    RequestedByUserId = actorId,
                    WorkspaceId = workspaceId
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
        var workspaceId = User.GetWorkspaceId();

        try
        {
            await _shareDocument.ExecuteAsync(
                new ShareDocumentRequest
                {
                    DocumentId = id,
                    TargetUserId = request.TargetUserId,
                    GrantedByUserId = actorId,
                    WorkspaceId = workspaceId
                },
                User.IsAdmin(),
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
        var workspaceId = User.GetWorkspaceId();

        try
        {
            await _shareTaskLinkedDocument.ExecuteAsync(
                new ShareTaskLinkedDocumentRequest
                {
                    DocumentId = id,
                    TaskId = taskId,
                    TargetUserId = request.TargetUserId,
                    GrantedByUserId = actorId,
                    WorkspaceId = workspaceId
                },
                User.IsAdmin(),
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

    [HttpDelete("{id:guid}/share/{userId:guid}")]
    public async Task<IActionResult> RevokeShare(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();
        var workspaceId = User.GetWorkspaceId();

        try
        {
            await _revokeDocumentAccess.ExecuteAsync(
                id,
                userId,
                actorId,
                workspaceId,
                User.IsAdmin(),
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
                new { message = "An unexpected error occurred while revoking document access." });
        }
    }

[HttpGet("{id:guid}")]
public async Task<IActionResult> GetMetadata(
    Guid id,
    CancellationToken cancellationToken)
{
    var actorId = User.GetActorId();
    var workspaceId = User.GetWorkspaceId();

    if (User.IsAdmin())
    {
        var adminDocument = await _documentRepository.GetByIdInWorkspaceAsync(id, workspaceId, cancellationToken);
        if (adminDocument is null)
        {
            return NotFound(new { message = "Document not found" });
        }

        return Ok(new DocumentMetadataDto(
            adminDocument.Id,
            adminDocument.OriginalFileName,
            adminDocument.ContentType,
            adminDocument.SizeInBytes,
            adminDocument.OwnerId,
            adminDocument.UploadedAtUtc,
            adminDocument.LinkedTaskId));
    }

    try
    {
        var allowTaskParticipationAccess = User.IsManager();

        var metadata = await _getDocumentMetadata.ExecuteAsync(
            id,
            actorId,
            workspaceId,
            allowTaskParticipationAccess,
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
        var workspaceId = User.GetWorkspaceId();

        try
        {
            var result = await _downloadDocument.ExecuteAsync(
                id,
                actorId,
                workspaceId,
                User.IsAdmin(),
                cancellationToken);

            return File(result.Content, result.ContentType, result.FileName);
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
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();
        var workspaceId = User.GetWorkspaceId();

        if (User.IsAdmin())
        {
            var adminDocument = await _documentRepository.GetByIdInWorkspaceAsync(id, workspaceId, cancellationToken);
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
            await _deleteDocument.ExecuteAsync(id, actorId, workspaceId, cancellationToken);
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

    public sealed class LinkDocumentToTaskBody
    {
        public Guid TaskId { get; init; }
    }

    public sealed class DocumentListRequest
    {
        public string? SearchTerm { get; init; }
        public string? ContentType { get; init; }
        public DateTime? UploadedFromUtc { get; init; }
        public DateTime? UploadedToUtc { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = DocumentQuery.DefaultPageSize;
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

    private bool CanManageTaskLinkedDocument(Document document, Domain.Tasks.TaskItem task, Guid actorId)
    {
        if (User.IsAdmin())
        {
            return true;
        }

        if (User.IsManager())
        {
            return document.OwnerId == actorId &&
                   (task.OwnerId == actorId || task.AssignedToUserId == actorId);
        }

        return document.OwnerId == actorId &&
               task.OwnerId == actorId;
    }
}
