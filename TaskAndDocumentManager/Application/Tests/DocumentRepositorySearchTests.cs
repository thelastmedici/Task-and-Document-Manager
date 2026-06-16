using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Infrastructure.Documents;

namespace TaskAndDocumentManager.Application.Tests.Documents.Infrastructure;

public class DocumentRepositorySearchTests
{
    [Fact]
    public async Task SearchDocumentsAsync_ShouldFilterByOriginalFileNameUsingContains()
    {
        var repository = new DocumentRepository();
        var ownerId = Guid.NewGuid();
        var searchToken = $"needle-{Guid.NewGuid():N}";
        var matchingDocument = new Document(
            $"quarterly-{searchToken}-report.pdf",
            "application/pdf",
            128,
            $"/tmp/{searchToken}.pdf",
            ownerId);
        var nonMatchingDocument = new Document(
            $"notes-{Guid.NewGuid():N}.pdf",
            "application/pdf",
            256,
            "/tmp/notes.pdf",
            ownerId);

        await repository.AddAsync(matchingDocument);
        await repository.AddAsync(nonMatchingDocument);

        try
        {
            var result = await repository.SearchDocumentsAsync(
                new DocumentQuery(SearchTerm: searchToken),
                CancellationToken.None);

            Assert.Contains(result, document => document.Id == matchingDocument.Id);
            Assert.DoesNotContain(result, document => document.Id == nonMatchingDocument.Id);
        }
        finally
        {
            await repository.DeleteAsync(matchingDocument.Id);
            await repository.DeleteAsync(nonMatchingDocument.Id);
        }
    }

    [Fact]
    public async Task SearchDocumentsPageAsync_ShouldReturnTotalCountBeforePaging()
    {
        var repository = new DocumentRepository();
        var ownerId = Guid.NewGuid();
        var searchToken = $"page-{Guid.NewGuid():N}";
        var firstDocument = new Document(
            $"first-{searchToken}.pdf",
            "application/pdf",
            128,
            $"/tmp/first-{searchToken}.pdf",
            ownerId);
        var secondDocument = new Document(
            $"second-{searchToken}.pdf",
            "application/pdf",
            256,
            $"/tmp/second-{searchToken}.pdf",
            ownerId);

        await repository.AddAsync(firstDocument);
        await repository.AddAsync(secondDocument);

        try
        {
            var result = await repository.SearchDocumentsPageAsync(
                new DocumentQuery(SearchTerm: searchToken, PageNumber: 1, PageSize: 1),
                CancellationToken.None);

            Assert.Single(result.Items);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(1, result.Page);
            Assert.Equal(1, result.PageSize);
        }
        finally
        {
            await repository.DeleteAsync(firstDocument.Id);
            await repository.DeleteAsync(secondDocument.Id);
        }
    }
}
