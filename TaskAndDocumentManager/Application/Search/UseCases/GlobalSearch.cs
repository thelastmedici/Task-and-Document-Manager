using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Search.DTOs;
using TaskAndDocumentManager.Application.Tasks.DTOs;
using TaskAndDocumentManager.Application.Tasks.UseCases;

namespace TaskAndDocumentManager.Application.Search.UseCases;

public class GlobalSearch
{
    private readonly ListTasks _listTasks;
    private readonly ListAccessibleDocuments _listAccessibleDocuments;

    public GlobalSearch(
        ListTasks listTasks,
        ListAccessibleDocuments listAccessibleDocuments)
    {
        _listTasks = listTasks;
        _listAccessibleDocuments = listAccessibleDocuments;
    }

    public async Task<GlobalSearchResult> ExecuteAsync(
        GlobalSearchQuery query,
        Guid actorId,
        Guid workspaceId,
        bool isAdmin,
        bool isManager,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);

        var tasks = await _listTasks.ExecuteAsync(
            new TaskQuery(
                PageNumber: normalizedQuery.PageNumber,
                PageSize: normalizedQuery.PageSize,
                SearchTerm: normalizedQuery.SearchTerm),
            actorId,
            workspaceId,
            isAdmin,
            isManager,
            cancellationToken);

        var documentQuery = new DocumentQuery(
            SearchTerm: normalizedQuery.SearchTerm,
            PageNumber: normalizedQuery.PageNumber,
            PageSize: normalizedQuery.PageSize);

        var documents = isAdmin
            ? await _listAccessibleDocuments.ExecuteForAdminAsync(workspaceId, documentQuery, cancellationToken)
            : await _listAccessibleDocuments.ExecuteAsync(
                actorId,
                workspaceId,
                isManager,
                documentQuery,
                cancellationToken);

        return new GlobalSearchResult(
            normalizedQuery.SearchTerm!,
            tasks.TotalCount + documents.TotalCount,
            tasks,
            documents);
    }

    private static GlobalSearchQuery NormalizeQuery(GlobalSearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            throw new ArgumentException("Search term is required.", nameof(query));
        }

        return query with
        {
            SearchTerm = query.SearchTerm.Trim(),
            PageNumber = query.PageNumber < 1 ? 1 : query.PageNumber,
            PageSize = query.PageSize < 1
                ? GlobalSearchQuery.DefaultPageSize
                : Math.Min(query.PageSize, GlobalSearchQuery.MaxPageSize)
        };
    }
}
