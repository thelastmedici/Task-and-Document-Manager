using Moq;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.Services;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class ListAccessibleDocumentsTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly ListAccessibleDocuments _sut;

    public ListAccessibleDocumentsTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();

        var documentAccessEvaluator = new DocumentAccessEvaluator(
            _documentAccessRepositoryMock.Object,
            _taskRepositoryMock.Object);

        _sut = new ListAccessibleDocuments(
            _documentRepositoryMock.Object,
            documentAccessEvaluator);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOwnedSharedAndTaskLinkedDocuments()
    {
        var requesterId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var ownDocument = new Document("own.pdf", "application/pdf", 128, "/tmp/own.pdf", requesterId);
        var sharedDocument = new Document("shared.pdf", "application/pdf", 256, "/tmp/shared.pdf", ownerId);
        var taskLinkedDocument = new Document("task.pdf", "application/pdf", 512, "/tmp/task.pdf", ownerId);
        var inaccessibleDocument = new Document("secret.pdf", "application/pdf", 1024, "/tmp/secret.pdf", ownerId);

        var task = new TaskItem("Review", "Review task document", ownerId);
        task.AssignTask(requesterId);
        taskLinkedDocument.LinkToTask(task.Id);

        _documentRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ownDocument, sharedDocument, taskLinkedDocument, inaccessibleDocument });

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(sharedDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(taskLinkedDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(inaccessibleDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _taskRepositoryMock
            .Setup(repository => repository.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _sut.ExecuteAsync(requesterId, true);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, document => document.Id == ownDocument.Id);
        Assert.Contains(result, document => document.Id == sharedDocument.Id);
        Assert.Contains(result, document => document.Id == taskLinkedDocument.Id);
        Assert.DoesNotContain(result, document => document.Id == inaccessibleDocument.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExcludeTaskLinkedDocuments_WhenTaskAccessIsDisabled()
    {
        var requesterId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var ownDocument = new Document("own.pdf", "application/pdf", 128, "/tmp/own.pdf", requesterId);
        var taskLinkedDocument = new Document("task.pdf", "application/pdf", 512, "/tmp/task.pdf", ownerId);

        var task = new TaskItem("Review", "Review task document", ownerId);
        task.AssignTask(requesterId);
        taskLinkedDocument.LinkToTask(task.Id);

        _documentRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ownDocument, taskLinkedDocument });

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(taskLinkedDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.ExecuteAsync(requesterId);

        Assert.Single(result);
        Assert.Contains(result, document => document.Id == ownDocument.Id);
        Assert.DoesNotContain(result, document => document.Id == taskLinkedDocument.Id);
    }
}
