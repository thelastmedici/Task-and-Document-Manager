using Moq;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.Services;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class GetDocumentMetadataTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly GetDocumentMetadata _sut;

    public GetDocumentMetadataTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();
        var documentAccessEvaluator = new DocumentAccessEvaluator(
            _documentAccessRepositoryMock.Object,
            _taskRepositoryMock.Object);
        _sut = new GetDocumentMetadata(_documentRepositoryMock.Object, documentAccessEvaluator);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnMetadata_WhenRequesterIsOwner()
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

        var result = await _sut.ExecuteAsync(document.Id, ownerId);

        Assert.Equal(document.Id, result.Id);
        Assert.Equal(document.OriginalFileName, result.FileName);
        Assert.Equal(document.SizeInBytes, result.SizeInBytes);
        Assert.Equal(ownerId, result.UploadedByUserId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenRequesterHasNoAccess()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var document = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/tmp/report.pdf",
            ownerId);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(document.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(document.Id, requesterId));

        Assert.Equal("You do not have access to this document.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnMetadata_WhenRequesterWasSharedOnDocument()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var document = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/tmp/report.pdf",
            ownerId);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(document.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.ExecuteAsync(document.Id, requesterId);

        Assert.Equal(document.Id, result.Id);
        Assert.Equal(document.OriginalFileName, result.FileName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnMetadata_WhenRequesterIsLinkedTaskParticipant()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var task = new TaskItem("Review", "Review linked document", ownerId);
        task.AssignTask(requesterId);

        var document = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/tmp/report.pdf",
            ownerId);
        document.LinkToTask(task.Id);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(document.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _taskRepositoryMock
            .Setup(repository => repository.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _sut.ExecuteAsync(document.Id, requesterId, true);

        Assert.Equal(document.Id, result.Id);
        Assert.Equal(document.OriginalFileName, result.FileName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenRequesterIsTaskParticipantButTaskAccessIsDisabled()
    {
        var ownerId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var task = new TaskItem("Review", "Review linked document", ownerId);
        task.AssignTask(requesterId);

        var document = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/tmp/report.pdf",
            ownerId);
        document.LinkToTask(task.Id);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(document.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(document.Id, requesterId));

        Assert.Equal("You do not have access to this document.", exception.Message);
    }
}
