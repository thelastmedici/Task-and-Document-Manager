using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Controllers;
using TaskAndDocumentManager.Infrastructure.Storage;

namespace TaskAndDocumentManager.Application.Tests.Documents.Controllers;

public class DocumentUploadSecurityTests
{
    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileIsMissing()
    {
        var controller = CreateController();
        SetUser(controller, Guid.NewGuid(), "User");

        var request = new UploadDocumentFormRequest
        {
            File = null
        };

        var result = await controller.Upload(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileIsEmpty()
    {
        var controller = CreateController();
        SetUser(controller, Guid.NewGuid(), "User");

        var fileMock = new Mock<IFormFile>();
        fileMock.SetupGet(file => file.Length).Returns(0);
        fileMock.SetupGet(file => file.FileName).Returns("report.pdf");

        var request = new UploadDocumentFormRequest
        {
            File = fileMock.Object
        };

        var result = await controller.Upload(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Upload_ShouldReturnCreatedAtAction_WithTypedResponse()
    {
        var ownerId = Guid.NewGuid();
        var controller = CreateController();
        SetUser(controller, ownerId, "User");

        var fileMock = new Mock<IFormFile>();
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        fileMock.SetupGet(file => file.Length).Returns(4);
        fileMock.SetupGet(file => file.FileName).Returns("report.pdf");
        fileMock.SetupGet(file => file.ContentType).Returns("application/pdf");
        fileMock.Setup(file => file.OpenReadStream()).Returns(stream);

        var request = new UploadDocumentFormRequest
        {
            File = fileMock.Object
        };

        _ = Mock.Get(GetFileStorageService(controller))
            .Setup(storage => storage.SaveAsync(
                ownerId,
                "report.pdf",
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("/tmp/report.pdf");

        var result = await controller.Upload(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(DocumentsController.GetMetadata), createdResult.ActionName);

        var response = Assert.IsType<UploadDocumentResponse>(createdResult.Value);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("report.pdf", response.FileName);
        Assert.Equal(response.Id, createdResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreFileInUserFolderWithSafeGuidFileName()
    {
        var sut = new FileStorageService();
        var userId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3, 4 };

        await using var input = new MemoryStream(content);
        var storagePath = await sut.SaveAsync(userId, "resume.pdf", input, CancellationToken.None);

        try
        {
            Assert.True(File.Exists(storagePath));
            Assert.Contains(Path.Combine("storage", "uploads", userId.ToString()), storagePath);

            var storedFileName = Path.GetFileName(storagePath);
            Assert.Matches("^[a-f0-9]{32}\\.pdf$", storedFileName);
        }
        finally
        {
            if (File.Exists(storagePath))
            {
                File.Delete(storagePath);
            }

            var userFolder = Path.GetDirectoryName(storagePath);
            if (!string.IsNullOrWhiteSpace(userFolder) && Directory.Exists(userFolder))
            {
                Directory.Delete(userFolder, recursive: false);
            }
        }
    }

    private static DocumentsController CreateController()
    {
        var documentRepositoryMock = new Mock<IDocumentRepository>();
        var documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        var taskRepositoryMock = new Mock<ITaskRepository>();
        var fileStorageServiceMock = new Mock<IFileStorageService>();
        var auditLogRepositoryMock = new Mock<IAuditLogRepository>();

        var uploadDocument = new UploadDocument(
            auditLogRepositoryMock.Object,
            documentRepositoryMock.Object,
            fileStorageServiceMock.Object);
        var linkDocumentToTask = new LinkDocumentToTask(documentRepositoryMock.Object, taskRepositoryMock.Object);
        var shareDocument = new ShareDocument(documentRepositoryMock.Object, documentAccessRepositoryMock.Object);
        var shareTaskLinkedDocument = new ShareTaskLinkedDocument(
            documentRepositoryMock.Object,
            documentAccessRepositoryMock.Object,
            taskRepositoryMock.Object);
        var revokeDocumentAccess = new RevokeDocumentAccess(
            documentRepositoryMock.Object,
            documentAccessRepositoryMock.Object);
        var getSharedDocuments = new GetSharedDocuments(
            documentRepositoryMock.Object,
            documentAccessRepositoryMock.Object);
        var documentAccessEvaluator = new Application.Documents.Services.DocumentAccessEvaluator(
            documentAccessRepositoryMock.Object,
            taskRepositoryMock.Object);
        var downloadDocument = new DownloadDocument(
            documentRepositoryMock.Object,
            documentAccessRepositoryMock.Object,
            fileStorageServiceMock.Object,
            NullLogger<DownloadDocument>.Instance);
        var deleteDocument = new DeleteDocument(
            auditLogRepositoryMock.Object,
            documentRepositoryMock.Object,
            fileStorageServiceMock.Object);
        var getDocumentMetadata = new GetDocumentMetadata(documentRepositoryMock.Object, documentAccessEvaluator);
        var listAccessibleDocuments = new ListAccessibleDocuments(documentRepositoryMock.Object, documentAccessEvaluator);

        return new DocumentsController(
            documentRepositoryMock.Object,
            documentAccessRepositoryMock.Object,
            taskRepositoryMock.Object,
            fileStorageServiceMock.Object,
            uploadDocument,
            linkDocumentToTask,
            shareDocument,
            shareTaskLinkedDocument,
            revokeDocumentAccess,
            getSharedDocuments,
            downloadDocument,
            deleteDocument,
            getDocumentMetadata,
            listAccessibleDocuments);
    }

    private static IFileStorageService GetFileStorageService(DocumentsController controller)
    {
        var field = typeof(DocumentsController)
            .GetField("_fileStorageService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return Assert.IsAssignableFrom<IFileStorageService>(field?.GetValue(controller));
    }

    private static void SetUser(ControllerBase controller, Guid userId, string role)
    {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                },
                "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };
    }
}
