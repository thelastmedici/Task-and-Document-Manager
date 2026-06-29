using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Api.Routing;
using TaskAndDocumentManager.Application.Notifications.DTOs;
using TaskAndDocumentManager.Application.Notifications.UseCases;

namespace TaskAndDocumentManager.Controllers;

[Authorize(Policy = AppPolicies.Authenticated)]
[ApiController]
[Route(ApiRoutes.Notifications)]
public class NotificationsController : ControllerBase
{
    private readonly GetNotifications _getNotifications;
    private readonly MarkNotificationAsRead _markNotificationAsRead;

    public NotificationsController(
        GetNotifications getNotifications,
        MarkNotificationAsRead markNotificationAsRead)
    {
        _getNotifications = getNotifications;
        _markNotificationAsRead = markNotificationAsRead;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] NotificationListRequest request,
        CancellationToken cancellationToken)
    {
        var actorId = User.GetActorId();
        var workspaceId = User.GetWorkspaceId();
        var query = new NotificationQuery(request.PageNumber, request.PageSize);
        var notifications = await _getNotifications.ExecuteAsync(actorId, workspaceId, query, cancellationToken);
        return Ok(notifications);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var actorId = User.GetActorId();
            var workspaceId = User.GetWorkspaceId();
            await _markNotificationAsRead.ExecuteAsync(id, actorId, workspaceId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public sealed record NotificationListRequest(
        int PageNumber = 1,
        int PageSize = NotificationQuery.DefaultPageSize);
}
