using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TaskAndDocumentManager.Application.Tasks.DTOs;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Domain.Tasks;
using Xunit;

namespace TaskAndDocumentManager.Application.Tests.Tasks.UseCases;

public class ListTasksTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly ListTasks _sut;

    public ListTasksTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _taskRepositoryMock
            .Setup(repo => repo.CountTasksAsync(It.IsAny<TaskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _sut = new ListTasks(_taskRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnTasksOrderedByCreatedAtDescending()
    {
        var olderTask = CreateTaskWithCreatedAt(new DateTime(2026, 03, 28, 9, 0, 0, DateTimeKind.Utc));
        var newerTask = CreateTaskWithCreatedAt(new DateTime(2026, 03, 29, 9, 0, 0, DateTimeKind.Utc));
        var cancellationToken = CancellationToken.None;
        var actorId = Guid.NewGuid();

        _taskRepositoryMock
            .Setup(repo => repo.SearchTasksAsync(It.IsAny<TaskQuery>(), cancellationToken))
            .ReturnsAsync(new List<TaskItem> { olderTask, newerTask });

        var result = await _sut.ExecuteAsync(new TaskQuery(), actorId, true, false, cancellationToken);

        Assert.Collection(
            result.Items,
            task => Assert.Equal(newerTask.Id, task.Id),
            task => Assert.Equal(olderTask.Id, task.Id));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnTaskDtos()
    {
        var task = CreateTaskWithCreatedAt(
            new DateTime(2026, 03, 29, 9, 0, 0, DateTimeKind.Utc),
            TaskPriority.High);
        var actorId = Guid.NewGuid();

        _taskRepositoryMock
            .Setup(repo => repo.SearchTasksAsync(It.IsAny<TaskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskItem> { task });

        var result = await _sut.ExecuteAsync(new TaskQuery(), actorId, true, false);

        var item = Assert.IsType<TaskListItemDto>(Assert.Single(result.Items));
        Assert.Equal(task.Title, item.Title);
        Assert.Equal(task.Description, item.Description);
        Assert.Equal(task.OwnerId, item.OwnerId);
        Assert.Equal(task.Priority, item.Priority);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnPaginationMetadata()
    {
        var actorId = Guid.NewGuid();
        var query = new TaskQuery(PageNumber: 2, PageSize: 5);

        _taskRepositoryMock
            .Setup(repo => repo.SearchTasksAsync(It.IsAny<TaskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TaskItem>());

        _taskRepositoryMock
            .Setup(repo => repo.CountTasksAsync(It.IsAny<TaskQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var result = await _sut.ExecuteAsync(query, actorId, true, false);

        Assert.Empty(result.Items);
        Assert.Equal(42, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizePaginationAndSearchBeforeQueryingRepository()
    {
        var query = new TaskQuery(PageNumber: 0, PageSize: 1000, SearchTerm: "  title  ");
        var actorId = Guid.NewGuid();

        _taskRepositoryMock
            .Setup(repo => repo.SearchTasksAsync(
                It.Is<TaskQuery>(requestedQuery =>
                    requestedQuery.PageNumber == 1 &&
                    requestedQuery.PageSize == TaskQuery.MaxPageSize &&
                    requestedQuery.SearchTerm == "title" &&
                    requestedQuery.OwnerId == null &&
                    requestedQuery.IncludeAssignedTasks == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TaskItem>());

        await _sut.ExecuteAsync(query, actorId, true, false);

        _taskRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassSearchFiltersAndSortingToRepository()
    {
        var ownerId = Guid.NewGuid();
        var assignedToUserId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var dueAfterUtc = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc);
        var dueBeforeUtc = new DateTime(2026, 04, 30, 23, 59, 59, DateTimeKind.Utc);
        var query = new TaskQuery(
            SearchTerm: "  report  ",
            AssignedToUserId: assignedToUserId,
            OwnerId: ownerId,
            Status: TaskStatusFilter.Completed,
            Priority: TaskPriority.High,
            DueAfterUtc: dueAfterUtc,
            DueBeforeUtc: dueBeforeUtc,
            SortBy: TaskSortBy.DueAt,
            SortDirection: SortDirection.Ascending);

        _taskRepositoryMock
            .Setup(repo => repo.SearchTasksAsync(
                It.Is<TaskQuery>(requestedQuery =>
                    requestedQuery.SearchTerm == "report" &&
                    requestedQuery.AssignedToUserId == assignedToUserId &&
                    requestedQuery.OwnerId == ownerId &&
                    requestedQuery.IsCompleted == true &&
                    requestedQuery.Status == TaskStatusFilter.Completed &&
                    requestedQuery.Priority == TaskPriority.High &&
                    requestedQuery.DueAfterUtc == dueAfterUtc &&
                    requestedQuery.DueBeforeUtc == dueBeforeUtc &&
                    requestedQuery.SortBy == TaskSortBy.DueAt &&
                    requestedQuery.SortDirection == SortDirection.Ascending),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TaskItem>());

        await _sut.ExecuteAsync(query, actorId, true, false);

        _taskRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenDueFromIsAfterDueTo()
    {
        var actorId = Guid.NewGuid();
        var query = new TaskQuery(
            DueAfterUtc: new DateTime(2026, 05, 02, 0, 0, 0, DateTimeKind.Utc),
            DueBeforeUtc: new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc));

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync(query, actorId, true, false));

        Assert.Equal("query", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldScopeQueryToOwner_WhenUserIsNotAdminOrManager()
    {
        var actorId = Guid.NewGuid();

        _taskRepositoryMock
            .Setup(repo => repo.SearchTasksAsync(
                It.Is<TaskQuery>(requestedQuery =>
                    requestedQuery.OwnerId == actorId &&
                    requestedQuery.IncludeAssignedTasks == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TaskItem>());

        await _sut.ExecuteAsync(new TaskQuery(), actorId, false, false);

        _taskRepositoryMock.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldScopeQueryToOwnerOrAssigned_WhenUserIsManager()
    {
        var actorId = Guid.NewGuid();

        _taskRepositoryMock
            .Setup(repo => repo.SearchTasksAsync(
                It.Is<TaskQuery>(requestedQuery =>
                    requestedQuery.OwnerId == actorId &&
                    requestedQuery.IncludeAssignedTasks),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TaskItem>());

        await _sut.ExecuteAsync(new TaskQuery(), actorId, false, true);

        _taskRepositoryMock.VerifyAll();
    }

    private static TaskItem CreateTaskWithCreatedAt(
        DateTime createdAt,
        TaskPriority priority = TaskPriority.Medium)
    {
        var task = new TaskItem("Title", "Description", Guid.NewGuid(), priority: priority);
        var createdAtProperty = typeof(TaskItem).GetProperty(
            nameof(TaskItem.CreatedAt),
            BindingFlags.Instance | BindingFlags.Public);

        createdAtProperty!.SetValue(task, createdAt);

        return task;
    }
}
