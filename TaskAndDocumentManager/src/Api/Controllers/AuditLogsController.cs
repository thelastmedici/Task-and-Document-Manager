using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Application.Audit.DTOs;
using TaskAndDocumentManager.Application.Audit.UseCases;

namespace TaskAndDocumentManager.Controllers;

[Authorize(Policy = AppPolicies.AdminOnly)]
[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly ListAuditLogs _listAuditLogs;

    public AuditLogsController(ListAuditLogs listAuditLogs)
    {
        _listAuditLogs = listAuditLogs;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] AuditLogListRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _listAuditLogs.ExecuteAsync(
                new AuditQuery(
                    request.PageNumber,
                    request.PageSize,
                    request.UserId,
                    request.Action,
                    request.TimestampFromUtc,
                    request.TimestampToUtc),
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public sealed class AuditLogListRequest
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = AuditQuery.DefaultPageSize;
        public Guid? UserId { get; init; }
        public string? Action { get; init; }
        public DateTime? TimestampFromUtc { get; init; }
        public DateTime? TimestampToUtc { get; init; }
    }
}
