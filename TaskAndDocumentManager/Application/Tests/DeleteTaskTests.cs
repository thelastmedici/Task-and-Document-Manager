using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Domain.Tasks;
using Xunit;

namespace TaskAndDocumentManager.Application.Tests.Tasks.UseCases;

public class DeleteTaskTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly DeleteTask _sut;

    public DeleteTaskTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _sut = new DeleteTask(_taskRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteTask_WhenTaskExists()
    {
        var taskId = Guid.NewGuid();
        var task = new TaskItem("Title", "Description", Guid.NewGuid());
        var cancellationToken = CancellationToken.None;

        typeof(TaskItem).GetProperty(nameof(TaskItem.Id))!.SetValue(task, taskId);

        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(taskId, cancellationToken))
            .ReturnsAsync(task);

        _taskRepositoryMock
            .Setup(repo => repo.DeleteAsync(taskId, cancellationToken))
            .Returns(Task.CompletedTask);

        await _sut.ExecuteAsync(taskId, cancellationToken);

        _taskRepositoryMock.Verify(repo => repo.GetByIdAsync(taskId, cancellationToken), Times.Once);
        _taskRepositoryMock.Verify(repo => repo.DeleteAsync(taskId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTaskDoesNotExist()
    {
        var taskId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(taskId, cancellationToken))
            .ReturnsAsync((TaskItem?)null);

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _sut.ExecuteAsync(taskId, cancellationToken));

        Assert.Equal("Task not found", exception.Message);
        _taskRepositoryMock.Verify(
            repo => repo.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
