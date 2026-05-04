using System.IO;
using Moq;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.Services;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class DownloadDocumentTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly DownloadDocument _sut;

    public DownloadDocumentTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();

        var documentAccessEvaluator = new DocumentAccessEvaluator(
            _documentAccessRepositoryMock.Object,
            _taskRepositoryMock.Object);

        _sut = new DownloadDocument(
            _documentRepositoryMock.Object,
            documentAccessEvaluator,
            _fileStorageServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnStream_WhenRequesterIsOwner()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 1024, "/tmp/report.pdf", ownerId);
        var expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _fileStorageServiceMock
            .Setup(storage => storage.OpenReadAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var result = await _sut.ExecuteAsync(document.Id, ownerId);

        Assert.Same(expectedStream, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnStream_WhenDocumentWasSharedWithRequester()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 1024, "/tmp/report.pdf", ownerId);
        var expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(document.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _fileStorageServiceMock
            .Setup(storage => storage.OpenReadAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var result = await _sut.ExecuteAsync(document.Id, requesterId);

        Assert.Same(expectedStream, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnStream_WhenRequesterIsTaskParticipantAndTaskAccessIsEnabled()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var task = new TaskItem("Review", "Review linked document", ownerId);
        task.AssignTask(requesterId);

        var document = new Document("report.pdf", "application/pdf", 1024, "/tmp/report.pdf", ownerId);
        document.LinkToTask(task.Id);

        var expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(document.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _taskRepositoryMock
            .Setup(repository => repository.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _fileStorageServiceMock
            .Setup(storage => storage.OpenReadAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var result = await _sut.ExecuteAsync(document.Id, requesterId, true);

        Assert.Same(expectedStream, result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenRequesterHasNoAccess()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 1024, "/tmp/report.pdf", ownerId);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(document.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(document.Id, requesterId));

        Assert.Equal("You do not have access to this document.", exception.Message);

        _fileStorageServiceMock.Verify(
            storage => storage.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
