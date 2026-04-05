using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.UseCases;

namespace TaskAndDocumentManager.Controllers;

[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly UploadDocument _uploadDocument;
    private readonly LinkDocumentToTask _linkDocumentToTask;
    private readonly ShareDocument _shareDocument;
    private readonly ShareTaskLinkedDocument _shareTaskLinkedDocument;
    private readonly DownloadDocument _downloadDocument;
    private readonly DeleteDocument _deleteDocument;
    private readonly GetDocumentMetadata _getDocumentMetadata;

    public DocumentsController(
        UploadDocument uploadDocument,
        LinkDocumentToTask linkDocumentToTask,
        ShareDocument shareDocument,
        ShareTaskLinkedDocument shareTaskLinkedDocument,
        DownloadDocument downloadDocument,
        DeleteDocument deleteDocument,
        GetDocumentMetadata getDocumentMetadata)
    {
        _uploadDocument = uploadDocument;
        _linkDocumentToTask = linkDocumentToTask;
        _shareDocument = shareDocument;
        _shareTaskLinkedDocument = shareTaskLinkedDocument;
        _downloadDocument = downloadDocument;
        _deleteDocument = deleteDocument;
        _getDocumentMetadata = getDocumentMetadata;
    }

    [HttpPost]
    [RequestSizeLimit(50_000_000)]
// for upload
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentFormRequest request,
        CancellationToken cancellationToken)
    {
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
                    UploadedByUserId = request.UploadedByUserId
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

    [HttpPost("{id:guid}/link-task")]
    public async Task<IActionResult> LinkToTask(
        Guid id,
        [FromBody] LinkDocumentToTaskBody request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _linkDocumentToTask.ExecuteAsync(
                new LinkDocumentToTaskRequest
                {
                    DocumentId = id,
                    TaskId = request.TaskId,
                    RequestedByUserId = request.RequestedByUserId
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
        try
        {
            await _shareDocument.ExecuteAsync(
                new ShareDocumentRequest
                {
                    DocumentId = id,
                    TargetUserId = request.TargetUserId,
                    GrantedByUserId = request.GrantedByUserId
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
        try
        {
            await _shareTaskLinkedDocument.ExecuteAsync(
                new ShareTaskLinkedDocumentRequest
                {
                    DocumentId = id,
                    TaskId = taskId,
                    TargetUserId = request.TargetUserId,
                    GrantedByUserId = request.GrantedByUserId
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
        [FromQuery] Guid requestedByUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _getDocumentMetadata.ExecuteAsync(id, requestedByUserId, cancellationToken);
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
        [FromQuery] Guid requestedByUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _getDocumentMetadata.ExecuteAsync(id, requestedByUserId, cancellationToken);
            var stream = await _downloadDocument.ExecuteAsync(id, requestedByUserId, cancellationToken);

            return File(stream, metadata.ContentType, metadata.FileName);
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
        [FromQuery] Guid requestedByUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _deleteDocument.ExecuteAsync(id, requestedByUserId, cancellationToken);
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
        public Guid UploadedByUserId { get; init; }
    }

    public sealed class LinkDocumentToTaskBody
    {
        public Guid TaskId { get; init; }
        public Guid RequestedByUserId { get; init; }
    }

    public sealed class ShareDocumentBody
    {
        public Guid TargetUserId { get; init; }
        public Guid GrantedByUserId { get; init; }
    }

    public sealed class ShareTaskLinkedDocumentBody
    {
        public Guid TargetUserId { get; init; }
        public Guid GrantedByUserId { get; init; }
    }
}
