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
        _sut = new ListTasks(_taskRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnTasksOrderedByCreatedAtDescending()
    {
        var olderTask = CreateTaskWithCreatedAt(new DateTime(2026, 03, 28, 9, 0, 0, DateTimeKind.Utc));
        var newerTask = CreateTaskWithCreatedAt(new DateTime(2026, 03, 29, 9, 0, 0, DateTimeKind.Utc));
        var cancellationToken = CancellationToken.None;

        _taskRepositoryMock
            .Setup(repo => repo.SearchAsync(It.IsAny<ListTasksQuery>(), cancellationToken))
            .ReturnsAsync(new List<TaskItem> { olderTask, newerTask });

        var result = await _sut.ExecuteAsync(new ListTasksQuery(), cancellationToken);

        Assert.Collection(
            result,
            task => Assert.Equal(newerTask.Id, task.Id),
            task => Assert.Equal(olderTask.Id, task.Id));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnTaskDtos()
    {
        var task = CreateTaskWithCreatedAt(new DateTime(2026, 03, 29, 9, 0, 0, DateTimeKind.Utc));

        _taskRepositoryMock
            .Setup(repo => repo.SearchAsync(It.IsAny<ListTasksQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskItem> { task });

        var result = await _sut.ExecuteAsync(new ListTasksQuery());

        var item = Assert.IsType<TaskListItemDto>(Assert.Single(result));
        Assert.Equal(task.Title, item.Title);
        Assert.Equal(task.Description, item.Description);
        Assert.Equal(task.CreatedByUserId, item.CreatedByUserId);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNormalizePaginationAndSearchBeforeQueryingRepository()
    {
        var query = new ListTasksQuery(PageNumber: 0, PageSize: 1000, SearchTerm: "  title  ");

        _taskRepositoryMock
            .Setup(repo => repo.SearchAsync(
                It.Is<ListTasksQuery>(requestedQuery =>
                    requestedQuery.PageNumber == 1 &&
                    requestedQuery.PageSize == ListTasksQuery.MaxPageSize &&
                    requestedQuery.SearchTerm == "title"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TaskItem>());

        await _sut.ExecuteAsync(query);

        _taskRepositoryMock.VerifyAll();
    }

    private static TaskItem CreateTaskWithCreatedAt(DateTime createdAt)
    {
        var task = new TaskItem("Title", "Description", Guid.NewGuid());
        var createdAtProperty = typeof(TaskItem).GetProperty(
            nameof(TaskItem.CreatedAt),
            BindingFlags.Instance | BindingFlags.Public);

        createdAtProperty!.SetValue(task, createdAt);

        return task;
    }
}
