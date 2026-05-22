using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskAndDocumentManager.Application.Audit.Interfaces;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class DownloadDocumentTests
{
    private readonly Mock<IAuditLogRepository> _auditLogRepositoryMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly DownloadDocument _sut;

    public DownloadDocumentTests()
    {
        _auditLogRepositoryMock = new Mock<IAuditLogRepository>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();

        _sut = new DownloadDocument(
            _auditLogRepositoryMock.Object,
            _documentRepositoryMock.Object,
            _documentAccessRepositoryMock.Object,
            _fileStorageServiceMock.Object,
            NullLogger<DownloadDocument>.Instance);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResult_WhenRequesterIsOwner()
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

        Assert.Same(expectedStream, result.Content);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("report.pdf", result.FileName);
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(auditLog =>
                    auditLog.UserId == ownerId &&
                    auditLog.Action == AuditActions.DocumentDownloaded &&
                    auditLog.EntityType == nameof(Document) &&
                    auditLog.EntityId == document.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResult_WhenRequesterIsAdmin()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 1024, "/tmp/report.pdf", ownerId);
        var expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _fileStorageServiceMock
            .Setup(storage => storage.OpenReadAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedStream);

        var result = await _sut.ExecuteAsync(document.Id, adminId, true);

        Assert.Same(expectedStream, result.Content);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("report.pdf", result.FileName);
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(auditLog =>
                    auditLog.UserId == adminId &&
                    auditLog.Action == AuditActions.DocumentDownloaded &&
                    auditLog.EntityType == nameof(Document) &&
                    auditLog.EntityId == document.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResult_WhenDocumentWasSharedWithRequester()
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

        Assert.Same(expectedStream, result.Content);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("report.pdf", result.FileName);
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<AuditLog>(auditLog =>
                    auditLog.UserId == requesterId &&
                    auditLog.Action == AuditActions.DocumentDownloaded &&
                    auditLog.EntityType == nameof(Document) &&
                    auditLog.EntityId == document.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenRequesterIsNotOwnerAndNotAdmin()
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
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowSafeFailure_WhenStoredFileIsMissing()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 1024, "/tmp/report.pdf", ownerId);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _fileStorageServiceMock
            .Setup(storage => storage.OpenReadAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("Stored document was not found.", document.StoragePath));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(document.Id, ownerId));

        Assert.Equal("Document could not be retrieved.", exception.Message);
        _auditLogRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
