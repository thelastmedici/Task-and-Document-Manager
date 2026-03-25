using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Domain.Entities;
using Xunit;

namespace TaskAndDocumentManager.Application.Tests.Tasks.UseCases;

public class CreateTaskTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly CreateTask _createTaskUseCase;

    public CreateTaskTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _createTaskUseCase = new CreateTask(_taskRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidData_ShouldCreateTaskAndReturnItsId()
    {
        var title = "Test Task";
        var description = "This is a test description.";
        var createdByUserId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _taskRepositoryMock
            .Setup(repo => repo.CreateAsync(It.IsAny<TaskItem>(), cancellationToken))
            .Returns(Task.CompletedTask);

        var taskId = await _createTaskUseCase.ExecuteAsync(title, description, createdByUserId, cancellationToken);

        Assert.NotEqual(Guid.Empty, taskId);

        _taskRepositoryMock.Verify(
            repo => repo.CreateAsync(
                It.Is<TaskItem>(task =>
                    task.Id == taskId &&
                    task.Title == title &&
                    task.Description == description &&
                    task.CreatedByUserId == createdByUserId &&
                    !task.IsCompleted),
                cancellationToken),
            Times.Once);
    }
}
