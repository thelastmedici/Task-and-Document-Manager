using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Domain.Documents;
using Xunit;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class DeleteDocumentTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly DeleteDocument _sut;

    public DeleteDocumentTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _sut = new DeleteDocument(_documentRepositoryMock.Object, _fileStorageServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteFileAndRecord_WhenRequesterOwnsDocument()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/tmp/report.pdf",
            ownerId);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        await _sut.ExecuteAsync(document.Id, ownerId, CancellationToken.None);

        _fileStorageServiceMock.Verify(
            storage => storage.DeleteAsync(document.StoragePath, It.IsAny<CancellationToken>()),
            Times.Once);

        _documentRepositoryMock.Verify(
            repository => repository.DeleteAsync(document.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenDocumentDoesNotExist()
    {
        var requesterId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _sut.ExecuteAsync(documentId, requesterId, CancellationToken.None));

        Assert.Equal("Document not found.", exception.Message);

        _fileStorageServiceMock.Verify(
            storage => storage.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _documentRepositoryMock.Verify(
            repository => repository.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenRequesterDoesNotOwnDocument()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var document = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/tmp/report.pdf",
            ownerId);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(document.Id, otherUserId, CancellationToken.None));

        Assert.Equal("Only the owner can delete this document.", exception.Message);

        _fileStorageServiceMock.Verify(
            storage => storage.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _documentRepositoryMock.Verify(
            repository => repository.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
