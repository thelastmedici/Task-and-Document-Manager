using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Api.Routing;
using TaskAndDocumentManager.Application.Workspaces.UseCases;

namespace TaskAndDocumentManager.Controllers;

[Authorize(Policy = AppPolicies.Authenticated)]
[ApiController]
[Route(ApiRoutes.Teams)]
public class TeamsController : ControllerBase
{
    private readonly AddTeamMember _addTeamMember;
    private readonly CreateTeam _createTeam;
    private readonly ListTeams _listTeams;
    private readonly RemoveTeamMember _removeTeamMember;

    public TeamsController(
        AddTeamMember addTeamMember,
        CreateTeam createTeam,
        ListTeams listTeams,
        RemoveTeamMember removeTeamMember)
    {
        _addTeamMember = addTeamMember;
        _createTeam = createTeam;
        _listTeams = listTeams;
        _removeTeamMember = removeTeamMember;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var team = await _createTeam.ExecuteAsync(
                User.GetWorkspaceId(),
                User.GetActorId(),
                request.Name,
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, team);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        try
        {
            var teams = await _listTeams.ExecuteAsync(
                User.GetWorkspaceId(),
                User.GetActorId(),
                cancellationToken);

            return Ok(teams);
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

    [HttpPost("{teamId:guid}/members")]
    public async Task<IActionResult> AddMember(
        Guid teamId,
        [FromBody] AddTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _addTeamMember.ExecuteAsync(
                User.GetWorkspaceId(),
                User.GetActorId(),
                teamId,
                request.UserId,
                cancellationToken);

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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{teamId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _removeTeamMember.ExecuteAsync(
                User.GetWorkspaceId(),
                User.GetActorId(),
                teamId,
                userId,
                cancellationToken);

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

    public sealed class CreateTeamRequest
    {
        public required string Name { get; init; }
    }

    public sealed class AddTeamMemberRequest
    {
        public Guid UserId { get; init; }
    }
}
