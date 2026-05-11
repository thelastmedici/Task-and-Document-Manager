using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Domain.Entities;
using Xunit;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class UploadDocumentTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly UploadDocument _sut;

    public UploadDocumentTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _sut = new UploadDocument(_documentRepositoryMock.Object, _fileStorageServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateDocument_WhenRequestIsValid()
    {
        var ownerId = Guid.NewGuid();
        await using var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var request = new UploadDocumentRequest
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            Content = content,
            SizeInBytes = 4,
            UploadedByUserId = ownerId
        };

        _fileStorageServiceMock
            .Setup(storage => storage.SaveAsync(
                ownerId,
                request.FileName,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("/tmp/report.pdf");

        var result = await _sut.ExecuteAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result);

        _documentRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<Document>(document =>
                    document.Id == result &&
                    document.OriginalFileName == request.FileName &&
                    document.ContentType == request.ContentType &&
                    document.SizeInBytes == request.SizeInBytes &&
                    document.OwnerId == ownerId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassUploadedByUserIdToStorageService()
    {
        var ownerId = Guid.NewGuid();
        await using var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var request = new UploadDocumentRequest
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            Content = content,
            SizeInBytes = 4,
            UploadedByUserId = ownerId
        };

        _fileStorageServiceMock
            .Setup(storage => storage.SaveAsync(
                ownerId,
                request.FileName,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("/storage/uploads/" + ownerId + "/saved-report.pdf");

        await _sut.ExecuteAsync(request, CancellationToken.None);

        _fileStorageServiceMock.Verify(
            storage => storage.SaveAsync(
                ownerId,
                request.FileName,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteSavedFile_WhenMetadataPersistenceFails()
    {
        var ownerId = Guid.NewGuid();
        await using var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var request = new UploadDocumentRequest
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            Content = content,
            SizeInBytes = 4,
            UploadedByUserId = ownerId
        };

        _fileStorageServiceMock
            .Setup(storage => storage.SaveAsync(
                ownerId,
                request.FileName,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("/tmp/report.pdf");

        _documentRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database save failed."));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("Database save failed.", exception.Message);

        _fileStorageServiceMock.Verify(
            storage => storage.DeleteAsync("/tmp/report.pdf", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenUploadedByUserIdIsEmpty()
    {
        await using var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var request = new UploadDocumentRequest
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            Content = content,
            SizeInBytes = 4,
            UploadedByUserId = Guid.Empty
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("UploadedByUserId", exception.ParamName);

        _fileStorageServiceMock.Verify(
            storage => storage.SaveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _documentRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenFileTypeIsNotAllowed()
    {
        await using var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var request = new UploadDocumentRequest
        {
            FileName = "malware.exe",
            ContentType = "application/octet-stream",
            Content = content,
            SizeInBytes = 4,
            UploadedByUserId = Guid.NewGuid()
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("FileName", exception.ParamName);

        _fileStorageServiceMock.Verify(
            storage => storage.SaveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenFileSizeExceedsLimit()
    {
        await using var content = new MemoryStream(new byte[] { 1 });

        var request = new UploadDocumentRequest
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            Content = content,
            SizeInBytes = (10 * 1024 * 1024) + 1,
            UploadedByUserId = Guid.NewGuid()
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("SizeInBytes", exception.ParamName);

        _fileStorageServiceMock.Verify(
            storage => storage.SaveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRethrowOriginalPersistenceException_WhenCleanupFails()
    {
        var ownerId = Guid.NewGuid();
        await using var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        var request = new UploadDocumentRequest
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            Content = content,
            SizeInBytes = 4,
            UploadedByUserId = ownerId
        };

        _fileStorageServiceMock
            .Setup(storage => storage.SaveAsync(
                ownerId,
                request.FileName,
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("/tmp/report.pdf");

        _documentRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database save failed."));

        _fileStorageServiceMock
            .Setup(storage => storage.DeleteAsync("/tmp/report.pdf", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Cleanup failed."));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(request, CancellationToken.None));

        Assert.Equal("Database save failed.", exception.Message);
    }
}
