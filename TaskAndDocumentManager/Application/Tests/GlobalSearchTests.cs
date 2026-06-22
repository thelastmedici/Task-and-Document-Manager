using Moq;
using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.Services;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Search.DTOs;
using TaskAndDocumentManager.Application.Search.UseCases;
using TaskAndDocumentManager.Application.Tasks.DTOs;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tests.Search.UseCases;

public class GlobalSearchTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly GlobalSearch _sut;

    public GlobalSearchTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();

        var listTasks = new ListTasks(_taskRepositoryMock.Object);
        var documentAccessEvaluator = new DocumentAccessEvaluator(
            _documentAccessRepositoryMock.Object,
            _taskRepositoryMock.Object);
        var listDocuments = new ListAccessibleDocuments(
            _documentRepositoryMock.Object,
            documentAccessEvaluator);

        _sut = new GlobalSearch(listTasks, listDocuments);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPermissionScopedWorkspaceResults_ForUser()
    {
        var actorId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var task = new TaskItem("Quarterly report", "Prepare the report", actorId, workspaceId);
        var ownedDocument = new Document("owned-report.pdf", "application/pdf", 128, "/tmp/owned.pdf", actorId, workspaceId);
        var sharedDocument = new Document("shared-report.pdf", "application/pdf", 256, "/tmp/shared.pdf", otherOwnerId, workspaceId);
        var privateDocument = new Document("private-report.pdf", "application/pdf", 512, "/tmp/private.pdf", otherOwnerId, workspaceId);

        _taskRepositoryMock
            .Setup(repository => repository.SearchTasksAsync(
                It.Is<TaskQuery>(query =>
                    query.SearchTerm == "report" &&
                    query.OwnerId == actorId &&
                    query.IncludeAssignedTasks == false &&
                    query.WorkspaceId == workspaceId &&
                    query.PageNumber == 1 &&
                    query.PageSize == GlobalSearchQuery.MaxPageSize),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([task]);

        _taskRepositoryMock
            .Setup(repository => repository.CountTasksAsync(
                It.Is<TaskQuery>(query =>
                    query.SearchTerm == "report" &&
                    query.OwnerId == actorId &&
                    query.WorkspaceId == workspaceId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _documentRepositoryMock
            .Setup(repository => repository.SearchDocumentsAsync(
                It.Is<DocumentQuery>(query =>
                    query.SearchTerm == "report" &&
                    query.WorkspaceId == workspaceId &&
                    query.PageNumber == 1 &&
                    query.PageSize == GlobalSearchQuery.MaxPageSize),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([ownedDocument, sharedDocument, privateDocument]);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(
                sharedDocument.Id,
                actorId,
                sharedDocument.WorkspaceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(
                privateDocument.Id,
                actorId,
                privateDocument.WorkspaceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.ExecuteAsync(
            new GlobalSearchQuery("  report  ", PageNumber: 0, PageSize: 1000),
            actorId,
            workspaceId,
            isAdmin: false,
            isManager: false);

        Assert.Equal("report", result.SearchTerm);
        Assert.Equal(3, result.TotalCount);
        Assert.Single(result.Tasks.Items);
        Assert.Equal(2, result.Documents.TotalCount);
        Assert.Contains(result.Documents.Items, document => document.Id == ownedDocument.Id);
        Assert.Contains(result.Documents.Items, document => document.Id == sharedDocument.Id);
        Assert.DoesNotContain(result.Documents.Items, document => document.Id == privateDocument.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSearchAllMatchingDocuments_ForAdmin()
    {
        var adminId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var task = new TaskItem("Report review", "Review report", ownerId, workspaceId);
        var firstDocument = new Document("first-report.pdf", "application/pdf", 128, "/tmp/first.pdf", ownerId, workspaceId);
        var secondDocument = new Document("second-report.pdf", "application/pdf", 256, "/tmp/second.pdf", Guid.NewGuid(), workspaceId);

        _taskRepositoryMock
            .Setup(repository => repository.SearchTasksAsync(
                It.Is<TaskQuery>(query =>
                    query.SearchTerm == "report" &&
                    query.OwnerId == null &&
                    query.WorkspaceId == workspaceId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([task]);

        _taskRepositoryMock
            .Setup(repository => repository.CountTasksAsync(
                It.IsAny<TaskQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _documentRepositoryMock
            .Setup(repository => repository.SearchDocumentsPageAsync(
                It.Is<DocumentQuery>(query =>
                    query.SearchTerm == "report" &&
                    query.WorkspaceId == workspaceId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResult<Document>(
                [firstDocument, secondDocument],
                2,
                1,
                GlobalSearchQuery.DefaultPageSize));

        var result = await _sut.ExecuteAsync(
            new GlobalSearchQuery("report"),
            adminId,
            workspaceId,
            isAdmin: true,
            isManager: false);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Documents.TotalCount);
        Assert.Contains(result.Documents.Items, document => document.Id == firstDocument.Id);
        Assert.Contains(result.Documents.Items, document => document.Id == secondDocument.Id);
        _documentAccessRepositoryMock.Verify(
            repository => repository.HasAccessAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectBlankSearchTerm()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(
                new GlobalSearchQuery("   "),
                Guid.NewGuid(),
                Guid.NewGuid(),
                isAdmin: false,
                isManager: false));

        Assert.Equal("query", exception.ParamName);
    }
}
