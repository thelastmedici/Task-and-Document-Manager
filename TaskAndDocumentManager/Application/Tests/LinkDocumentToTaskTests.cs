using Moq;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class LinkDocumentToTaskTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly LinkDocumentToTask _sut;

    public LinkDocumentToTaskTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _sut = new LinkDocumentToTask(_documentRepositoryMock.Object, _taskRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLinkDocumentToTask_WhenRequestIsValid()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/tmp/report.pdf",
            ownerId);
        var task = new TaskItem("Review report", "Review uploaded document", Guid.NewGuid());
        var request = new LinkDocumentToTaskRequest
        {
            DocumentId = document.Id,
            TaskId = task.Id,
            RequestedByUserId = ownerId
        };

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _taskRepositoryMock
            .Setup(repository => repository.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _documentRepositoryMock
            .Setup(repository => repository.UpdateAsync(document, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ExecuteAsync(request);

        Assert.Equal(task.Id, document.LinkedTaskId);
        _documentRepositoryMock.Verify(
            repository => repository.UpdateAsync(document, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenRequesterIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document(
            "report.pdf",
            "application/pdf",
            1024,
            "/tmp/report.pdf",
            ownerId);
        var request = new LinkDocumentToTaskRequest
        {
            DocumentId = document.Id,
            TaskId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid()
        };

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.ExecuteAsync(request));

        Assert.Equal("Only the owner can link this document to a task.", exception.Message);
        _documentRepositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Document>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
