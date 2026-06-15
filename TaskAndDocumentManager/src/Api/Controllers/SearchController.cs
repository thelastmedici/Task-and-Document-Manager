using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskAndDocumentManager.Api.Authorization;
using TaskAndDocumentManager.Api.Extensions;
using TaskAndDocumentManager.Application.Search.DTOs;
using TaskAndDocumentManager.Application.Search.UseCases;

namespace TaskAndDocumentManager.Controllers;

[Authorize(Policy = AppPolicies.Authenticated)]
[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly GlobalSearch _globalSearch;

    public SearchController(GlobalSearch globalSearch)
    {
        _globalSearch = globalSearch;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] GlobalSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _globalSearch.ExecuteAsync(
                new GlobalSearchQuery(
                    request.SearchTerm,
                    request.PageNumber,
                    request.PageSize),
                User.GetActorId(),
                User.IsAdmin(),
                User.IsManager(),
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public sealed class GlobalSearchRequest
    {
        public string? SearchTerm { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = GlobalSearchQuery.DefaultPageSize;
    }
}
