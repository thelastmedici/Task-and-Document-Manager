using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Tasks.DTOs;

namespace TaskAndDocumentManager.Application.Search.DTOs;

public sealed record GlobalSearchResult(
    string SearchTerm,
    int TotalCount,
    PaginatedResult<TaskListItemDto> Tasks,
    PaginatedResult<DocumentMetadataDto> Documents);
