using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Domain.Tasks;
using Xunit;

namespace TaskAndDocumentManager.Application.Tests.Tasks.UseCases;

public class UpdateTaskTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly UpdateTask _sut;

    public UpdateTaskTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _sut = new UpdateTask(_taskRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateTaskAndPersistChanges_WhenTaskExists()
    {
        var taskId = Guid.NewGuid();
        var task = new TaskItem("Old title", "Old description", Guid.NewGuid());
        var updatedTitle = "New title";
        var updatedDescription = "New description";
        var cancellationToken = CancellationToken.None;

        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(taskId, cancellationToken))
            .ReturnsAsync(task);

        _taskRepositoryMock
            .Setup(repo => repo.UpdateAsync(task, cancellationToken))
            .Returns(Task.CompletedTask);

        await _sut.ExecuteAsync(taskId, updatedTitle, updatedDescription, cancellationToken);

        Assert.Equal(updatedTitle, task.Title);
        Assert.Equal(updatedDescription, task.Description);
        Assert.NotNull(task.UpdatedAt);

        _taskRepositoryMock.Verify(repo => repo.GetByIdAsync(taskId, cancellationToken), Times.Once);
        _taskRepositoryMock.Verify(repo => repo.UpdateAsync(task, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTaskDoesNotExist()
    {
        var taskId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(taskId, cancellationToken))
            .ReturnsAsync((TaskItem?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(taskId, "Title", "Description", cancellationToken));

        Assert.Equal("Task not found", exception.Message);
        _taskRepositoryMock.Verify(
            repo => repo.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
