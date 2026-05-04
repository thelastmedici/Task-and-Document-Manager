using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Controllers;
using TaskAndDocumentManager.Infrastructure.Storage;

namespace TaskAndDocumentManager.Application.Tests.Documents.Controllers;

public class DocumentUploadSecurityTests
{
    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileExtensionIsNotAllowed()
    {
        var controller = CreateController();
        SetUser(controller, Guid.NewGuid(), "User");

        var fileMock = new Mock<IFormFile>();
        fileMock.SetupGet(file => file.Length).Returns(128);
        fileMock.SetupGet(file => file.FileName).Returns("malware.exe");

        var request = new DocumentsController.UploadDocumentFormRequest
        {
            File = fileMock.Object
        };

        var result = await controller.Upload(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenFileIsTooLarge()
    {
        var controller = CreateController();
        SetUser(controller, Guid.NewGuid(), "User");

        var fileMock = new Mock<IFormFile>();
        fileMock.SetupGet(file => file.Length).Returns(11L * 1024 * 1024);
        fileMock.SetupGet(file => file.FileName).Returns("report.pdf");

        var request = new DocumentsController.UploadDocumentFormRequest
        {
            File = fileMock.Object
        };

        var result = await controller.Upload(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
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

        var uploadDocument = new UploadDocument(documentRepositoryMock.Object, fileStorageServiceMock.Object);
        var linkDocumentToTask = new LinkDocumentToTask(documentRepositoryMock.Object, taskRepositoryMock.Object);
        var shareDocument = new ShareDocument(documentRepositoryMock.Object, documentAccessRepositoryMock.Object);
        var shareTaskLinkedDocument = new ShareTaskLinkedDocument(
            documentRepositoryMock.Object,
            documentAccessRepositoryMock.Object,
            taskRepositoryMock.Object);
        var documentAccessEvaluator = new Application.Documents.Services.DocumentAccessEvaluator(
            documentAccessRepositoryMock.Object,
            taskRepositoryMock.Object);
        var downloadDocument = new DownloadDocument(
            documentRepositoryMock.Object,
            documentAccessEvaluator,
            fileStorageServiceMock.Object);
        var deleteDocument = new DeleteDocument(documentRepositoryMock.Object, fileStorageServiceMock.Object);
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
            downloadDocument,
            deleteDocument,
            getDocumentMetadata,
            listAccessibleDocuments);
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
