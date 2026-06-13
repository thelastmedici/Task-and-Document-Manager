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
        var result = await _listAuditLogs.ExecuteAsync(
            new AuditLogQuery(request.PageNumber, request.PageSize),
            cancellationToken);

        return Ok(result);
    }

    public sealed class AuditLogListRequest
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = AuditLogQuery.DefaultPageSize;
    }
}
