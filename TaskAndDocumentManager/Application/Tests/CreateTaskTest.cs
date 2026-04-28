using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Domain.Tasks;
using Xunit;

namespace TaskAndDocumentManager.Application.Tests.Tasks.UseCases;

public class CreateTaskTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly CreateTask _sut; // System Under Test

    public CreateTaskTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _sut = new CreateTask(_taskRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateTaskAndReturnId_WhenInputIsValid()
    {
        // Arrange
        var title = "Test Task";
        var description = "This is a test description.";
        var ownerId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _taskRepositoryMock
            .Setup(repo => repo.CreateAsync(It.IsAny<TaskItem>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExecuteAsync(
            title,
            description,
            ownerId,
            cancellationToken);

        // Assert
        Assert.NotEqual(Guid.Empty, result);

        _taskRepositoryMock.Verify(repo =>
            repo.CreateAsync(
                It.Is<TaskItem>(task =>
                    task.Id == result &&
                    task.Title == title &&
                    task.Description == description &&
                    task.OwnerId == ownerId &&
                    task.IsCompleted == false),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenOwnerIdIsEmpty()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.ExecuteAsync("Test Task", "This is a test description.", Guid.Empty, CancellationToken.None));

        Assert.Equal("ownerId", exception.ParamName);

        _taskRepositoryMock.Verify(
            repo => repo.CreateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
