using System.IO;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Application.Tasks.Dtos;
using TaskAndDocumentManager.Application.Tasks.UseCases;

namespace TaskAndDocumentManager.Controllers;

[ApiController]
[Route("api/tasks")]
public class TaskController : ControllerBase
{
    private readonly CreateTask _createTask;
    private readonly ListTasks _listTasks;
    private readonly UpdateTask _updateTask;
    private readonly DeleteTask _deleteTask;
    private readonly AssignTask _assignTask;

    public TaskController(
        CreateTask createTask,
        ListTasks listTasks,
        UpdateTask updateTask,
        DeleteTask deleteTask,
        AssignTask assignTask)
    {
        _createTask = createTask;
        _listTasks = listTasks;
        _updateTask = updateTask;
        _deleteTask = deleteTask;
        _assignTask = assignTask;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var taskId = await _createTask.ExecuteAsync(
                request.Title,
                request.Description,
                request.CreatedByUserId,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, new
            {
                id = taskId,
                message = "Task created successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while creating the task." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] ListTasksRequest request,
        CancellationToken cancellationToken)
    {
        var query = new ListTasksQuery(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.IsCompleted,
            request.AssignedToUserId);

        var tasks = await _listTasks.ExecuteAsync(query, cancellationToken);
        return Ok(tasks);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _updateTask.ExecuteAsync(id, request.Title, request.Description, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message == "Task not found")
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while updating the task." });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _deleteTask.ExecuteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while deleting the task." });
        }
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignTaskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _assignTask.ExecuteAsync(id, request.UserId, cancellationToken);
            return NoContent();
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "An unexpected error occurred while assigning the task." });
        }
    }

    public sealed class CreateTaskRequest
    {
        public required string Title { get; init; }
        public required string Description { get; init; }
        public Guid CreatedByUserId { get; init; }
    }

    public sealed class UpdateTaskRequest
    {
        public required string Title { get; init; }
        public required string Description { get; init; }
    }

    public sealed class AssignTaskRequest
    {
        public Guid UserId { get; init; }
    }

    public sealed class ListTasksRequest
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = ListTasksQuery.DefaultPageSize;
        public string? SearchTerm { get; init; }
        public bool? IsCompleted { get; init; }
        public Guid? AssignedToUserId { get; init; }
    }
}
