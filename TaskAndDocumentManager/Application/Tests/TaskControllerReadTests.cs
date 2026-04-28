using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskAndDocumentManager.Application.Tasks.DTOs;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Application.Tasks.UseCases;
using TaskAndDocumentManager.Controllers;
using TaskAndDocumentManager.Domain.Tasks;
using Xunit;

namespace TaskAndDocumentManager.Application.Tests.Tasks.Controllers;

public class TaskControllerReadTests
{
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly TaskController _sut;

    public TaskControllerReadTests()
    {
        _taskRepositoryMock = new Mock<ITaskRepository>();

        _sut = new TaskController(
            new CreateTask(_taskRepositoryMock.Object),
            _taskRepositoryMock.Object,
            new ListTasks(_taskRepositoryMock.Object),
            new UpdateTask(_taskRepositoryMock.Object),
            new DeleteTask(_taskRepositoryMock.Object),
            new AssignTask(_taskRepositoryMock.Object));
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenUserOwnsTask()
    {
        var ownerId = Guid.NewGuid();
        var task = new TaskItem("Test Task", "Test Description", ownerId);

        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        SetUser(ownerId, "User");

        var result = await _sut.GetById(task.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TaskListItemDto>(okResult.Value);
        Assert.Equal(task.Id, dto.Id);
        Assert.Equal(ownerId, dto.OwnerId);
    }

    [Fact]
    public async Task GetById_ShouldReturnOk_WhenUserIsAdmin()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var task = new TaskItem("Admin Read", "Can read any task", ownerId);

        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        SetUser(adminId, "Admin");

        var result = await _sut.GetById(task.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TaskListItemDto>(okResult.Value);
        Assert.Equal(task.Id, dto.Id);
    }

    [Fact]
    public async Task GetById_ShouldReturnForbid_WhenUserDoesNotOwnTaskAndIsNotAdmin()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var task = new TaskItem("Private Task", "Not yours", ownerId);

        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        SetUser(otherUserId, "User");

        var result = await _sut.GetById(task.Id, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        var actorId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _taskRepositoryMock
            .Setup(repo => repo.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItem?)null);

        SetUser(actorId, "User");

        var result = await _sut.GetById(taskId, CancellationToken.None);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFoundResult.Value);
    }

    private void SetUser(Guid userId, string role)
    {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Role, role)
                },
                "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };
    }
}