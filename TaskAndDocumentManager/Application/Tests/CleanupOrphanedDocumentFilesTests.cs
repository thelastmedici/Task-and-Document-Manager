using Moq;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class CleanupOrphanedDocumentFilesTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IFileStorageMaintenanceService> _fileStorageMaintenanceServiceMock;
    private readonly CleanupOrphanedDocumentFiles _sut;

    public CleanupOrphanedDocumentFilesTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _fileStorageMaintenanceServiceMock = new Mock<IFileStorageMaintenanceService>();
        _sut = new CleanupOrphanedDocumentFiles(
            _documentRepositoryMock.Object,
            _fileStorageMaintenanceServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteStoredFiles_WhenNoDocumentMetadataReferencesThem()
    {
        var knownDocument = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/storage/uploads/user/report.pdf",
            Guid.NewGuid());
        var orphanPath = "/storage/uploads/user/orphan.pdf";

        _documentRepositoryMock
            .Setup(repository => repository.GetAllForMaintenanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { knownDocument });

        _fileStorageMaintenanceServiceMock
            .Setup(storage => storage.GetStoredFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { knownDocument.StoragePath, orphanPath });

        await _sut.ExecuteAsync(CancellationToken.None);

        _fileStorageMaintenanceServiceMock.Verify(
            storage => storage.DeleteAsync(orphanPath, It.IsAny<CancellationToken>()),
            Times.Once);
        _fileStorageMaintenanceServiceMock.Verify(
            storage => storage.DeleteAsync(knownDocument.StoragePath, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotDeleteFiles_WhenAllStoredFilesHaveMetadata()
    {
        var knownDocument = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/storage/uploads/user/report.pdf",
            Guid.NewGuid());

        _documentRepositoryMock
            .Setup(repository => repository.GetAllForMaintenanceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { knownDocument });

        _fileStorageMaintenanceServiceMock
            .Setup(storage => storage.GetStoredFilePathsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { knownDocument.StoragePath });

        await _sut.ExecuteAsync(CancellationToken.None);

        _fileStorageMaintenanceServiceMock.Verify(
            storage => storage.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
