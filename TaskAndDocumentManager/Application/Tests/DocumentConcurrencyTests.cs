using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Controllers;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;
using TaskAndDocumentManager.Infrastructure.Auth.Token;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Application.Tests;

public class DocumentConcurrencyTests
{
    [Fact]
    public void Document_ShouldExposeVersionToken_ForOptimisticConcurrency()
    {
        var document = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/tmp/report.pdf",
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.Equal(0u, document.Version);
    }

    [Fact]
    public async Task LinkToTask_ShouldReturnConflict_WhenSaveHitsConcurrencyError()
    {
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var documentRepository = new Mock<IDocumentRepository>();
        var taskRepository = new Mock<ITaskRepository>();
        var fileStorageService = new Mock<IFileStorageService>();
        var uploadDocument = new Mock<UploadDocument>(MockBehavior.Loose);
        var linkDocumentToTask = new Mock<LinkDocumentToTask>(MockBehavior.Loose);
        var shareDocument = new Mock<ShareDocument>(MockBehavior.Loose);
        var shareTaskLinkedDocument = new Mock<ShareTaskLinkedDocument>(MockBehavior.Loose);
        var revokeDocumentAccess = new Mock<RevokeDocumentAccess>(MockBehavior.Loose);
        var getSharedDocuments = new Mock<GetSharedDocuments>(MockBehavior.Loose);
        var downloadDocument = new Mock<DownloadDocument>(MockBehavior.Loose);
        var deleteDocument = new Mock<DeleteDocument>(MockBehavior.Loose);
        var getDocumentMetadata = new Mock<GetDocumentMetadata>(MockBehavior.Loose);
        var listAccessibleDocuments = new Mock<ListAccessibleDocuments>(MockBehavior.Loose);

        var document = new Document("report.pdf", "application/pdf", 1024, "/tmp/report.pdf", ownerId, workspaceId)
        {
            Id = documentId
        };

        documentRepository
            .Setup(repository => repository.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        taskRepository
            .Setup(repository => repository.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TaskItem("Review report", "Review upload", actorId, workspaceId));

        documentRepository
            .Setup(repository => repository.UpdateAsync(document, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Document version conflict."));

        var controller = new DocumentsController(
            documentRepository.Object,
            Mock.Of<IDocumentAccessRepository>(),
            taskRepository.Object,
            fileStorageService.Object,
            uploadDocument.Object,
            linkDocumentToTask.Object,
            shareDocument.Object,
            shareTaskLinkedDocument.Object,
            revokeDocumentAccess.Object,
            getSharedDocuments.Object,
            downloadDocument.Object,
            deleteDocument.Object,
            getDocumentMetadata.Object,
            listAccessibleDocuments.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
                    new Claim(ClaimTypes.Role, BuiltInRoles.AdminName),
                    new Claim(JwtTokenService.WorkspaceIdClaimType, workspaceId.ToString())
                }, "TestAuth"))
            }
        };

        var result = await controller.LinkToTask(
            documentId,
            new DocumentsController.LinkDocumentToTaskBody { TaskId = taskId },
            CancellationToken.None);

        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }
}
