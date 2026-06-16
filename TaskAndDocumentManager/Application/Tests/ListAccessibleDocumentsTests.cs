using Moq;
using System.Reflection;
using TaskAndDocumentManager.Application.Common.DTOs;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.Services;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Application.Tasks.Interfaces;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class ListAccessibleDocumentsTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly ListAccessibleDocuments _sut;

    public ListAccessibleDocumentsTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();

        var documentAccessEvaluator = new DocumentAccessEvaluator(
            _documentAccessRepositoryMock.Object,
            _taskRepositoryMock.Object);

        _sut = new ListAccessibleDocuments(
            _documentRepositoryMock.Object,
            documentAccessEvaluator);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOwnedSharedAndTaskLinkedDocuments()
    {
        var requesterId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var ownDocument = new Document("own.pdf", "application/pdf", 128, "/tmp/own.pdf", requesterId);
        var sharedDocument = new Document("shared.pdf", "application/pdf", 256, "/tmp/shared.pdf", ownerId);
        var taskLinkedDocument = new Document("task.pdf", "application/pdf", 512, "/tmp/task.pdf", ownerId);
        var inaccessibleDocument = new Document("secret.pdf", "application/pdf", 1024, "/tmp/secret.pdf", ownerId);

        var task = new TaskItem("Review", "Review task document", ownerId);
        task.AssignTask(requesterId);
        taskLinkedDocument.LinkToTask(task.Id);

        SetupDocumentSearch(ownDocument, sharedDocument, taskLinkedDocument, inaccessibleDocument);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(sharedDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(taskLinkedDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(inaccessibleDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _taskRepositoryMock
            .Setup(repository => repository.GetByIdAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _sut.ExecuteAsync(requesterId, true);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
        Assert.Contains(result.Items, document => document.Id == ownDocument.Id);
        Assert.Contains(result.Items, document => document.Id == sharedDocument.Id);
        Assert.Contains(result.Items, document => document.Id == taskLinkedDocument.Id);
        Assert.DoesNotContain(result.Items, document => document.Id == inaccessibleDocument.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExcludeTaskLinkedDocuments_WhenTaskAccessIsDisabled()
    {
        var requesterId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var ownDocument = new Document("own.pdf", "application/pdf", 128, "/tmp/own.pdf", requesterId);
        var taskLinkedDocument = new Document("task.pdf", "application/pdf", 512, "/tmp/task.pdf", ownerId);

        var task = new TaskItem("Review", "Review task document", ownerId);
        task.AssignTask(requesterId);
        taskLinkedDocument.LinkToTask(task.Id);

        SetupDocumentSearch(ownDocument, taskLinkedDocument);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(taskLinkedDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.ExecuteAsync(requesterId);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Contains(result.Items, document => document.Id == ownDocument.Id);
        Assert.DoesNotContain(result.Items, document => document.Id == taskLinkedDocument.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterAccessibleDocuments_ByOriginalFileName()
    {
        var requesterId = Guid.NewGuid();
        var reportDocument = new Document("quarterly-report.pdf", "application/pdf", 128, "/tmp/report.pdf", requesterId);
        var imageDocument = new Document("diagram.png", "image/png", 256, "/tmp/diagram.png", requesterId);

        SetupDocumentSearch(reportDocument, imageDocument);

        var result = await _sut.ExecuteAsync(
            requesterId,
            false,
            new DocumentQuery(SearchTerm: "report"),
            CancellationToken.None);

        var document = Assert.Single(result.Items);
        Assert.Equal(reportDocument.Id, document.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSearchOnlyOwnedAndSharedDocuments()
    {
        var requesterId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        var ownedDocument = new Document("owned-report.pdf", "application/pdf", 128, "/tmp/owned-report.pdf", requesterId);
        var sharedDocument = new Document("shared-report.pdf", "application/pdf", 256, "/tmp/shared-report.pdf", otherOwnerId);
        var inaccessibleDocument = new Document("private-report.pdf", "application/pdf", 512, "/tmp/private-report.pdf", otherOwnerId);
        var nonMatchingOwnedDocument = new Document("notes.pdf", "application/pdf", 1024, "/tmp/notes.pdf", requesterId);

        SetupDocumentSearch(
            ownedDocument,
            sharedDocument,
            inaccessibleDocument,
            nonMatchingOwnedDocument);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(sharedDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _documentAccessRepositoryMock
            .Setup(repository => repository.HasAccessAsync(inaccessibleDocument.Id, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.ExecuteAsync(
            requesterId,
            false,
            new DocumentQuery(SearchTerm: "report"),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, document => document.Id == ownedDocument.Id);
        Assert.Contains(result.Items, document => document.Id == sharedDocument.Id);
        Assert.DoesNotContain(result.Items, document => document.Id == inaccessibleDocument.Id);
        Assert.DoesNotContain(result.Items, document => document.Id == nonMatchingOwnedDocument.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterAccessibleDocuments_ByContentType()
    {
        var requesterId = Guid.NewGuid();
        var pdfDocument = new Document("report.pdf", "application/pdf", 128, "/tmp/report.pdf", requesterId);
        var imageDocument = new Document("diagram.png", "image/png", 256, "/tmp/diagram.png", requesterId);

        SetupDocumentSearch(pdfDocument, imageDocument);

        var result = await _sut.ExecuteAsync(
            requesterId,
            false,
            new DocumentQuery(ContentType: "image/png"),
            CancellationToken.None);

        var document = Assert.Single(result.Items);
        Assert.Equal(imageDocument.Id, document.Id);
    }

    [Fact]
    public async Task ExecuteForAdminAsync_ShouldFilterDocuments_ByUploadedDateRange()
    {
        var ownerId = Guid.NewGuid();
        var olderDocument = new Document("old.pdf", "application/pdf", 128, "/tmp/old.pdf", ownerId);
        var newerDocument = new Document("new.pdf", "application/pdf", 256, "/tmp/new.pdf", ownerId);
        SetUploadedAtUtc(olderDocument, new DateTime(2026, 05, 01, 10, 0, 0, DateTimeKind.Utc));
        SetUploadedAtUtc(newerDocument, new DateTime(2026, 05, 20, 10, 0, 0, DateTimeKind.Utc));

        SetupDocumentSearch(olderDocument, newerDocument);

        var result = await _sut.ExecuteForAdminAsync(
            new DocumentQuery(
                UploadedFromUtc: new DateTime(2026, 05, 10, 0, 0, 0, DateTimeKind.Utc),
                UploadedToUtc: new DateTime(2026, 05, 31, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        var document = Assert.Single(result.Items);
        Assert.Equal(newerDocument.Id, document.Id);
    }

    [Fact]
    public async Task ExecuteForAdminAsync_ShouldSearchAllMatchingDocuments()
    {
        var firstOwnerId = Guid.NewGuid();
        var secondOwnerId = Guid.NewGuid();

        var firstDocument = new Document("first-report.pdf", "application/pdf", 128, "/tmp/first-report.pdf", firstOwnerId);
        var secondDocument = new Document("second-report.pdf", "application/pdf", 256, "/tmp/second-report.pdf", secondOwnerId);
        var nonMatchingDocument = new Document("notes.pdf", "application/pdf", 512, "/tmp/notes.pdf", secondOwnerId);

        SetupDocumentSearch(firstDocument, secondDocument, nonMatchingDocument);

        var result = await _sut.ExecuteForAdminAsync(
            new DocumentQuery(SearchTerm: "report"),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, document => document.Id == firstDocument.Id);
        Assert.Contains(result.Items, document => document.Id == secondDocument.Id);
        Assert.DoesNotContain(result.Items, document => document.Id == nonMatchingDocument.Id);
    }

    [Fact]
    public async Task ExecuteForAdminAsync_ShouldReturnPaginationMetadata()
    {
        var ownerId = Guid.NewGuid();
        var firstDocument = new Document("first.pdf", "application/pdf", 128, "/tmp/first.pdf", ownerId);
        var secondDocument = new Document("second.pdf", "application/pdf", 256, "/tmp/second.pdf", ownerId);

        SetupDocumentSearch(firstDocument, secondDocument);

        var result = await _sut.ExecuteForAdminAsync(
            new DocumentQuery(PageNumber: 2, PageSize: 1),
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(1, result.PageSize);
    }

    private static void SetUploadedAtUtc(Document document, DateTime uploadedAtUtc)
    {
        var property = typeof(Document).GetProperty(
            nameof(Document.UploadedAtUtc),
            BindingFlags.Instance | BindingFlags.Public);

        property!.SetValue(document, uploadedAtUtc);
    }

    private void SetupDocumentSearch(params Document[] documents)
    {
        _documentRepositoryMock
            .Setup(repository => repository.SearchDocumentsAsync(
                It.IsAny<DocumentQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentQuery query, CancellationToken _) =>
                ApplySearch(documents, NormalizeQuery(query))
                    .OrderByDescending(document => document.UploadedAtUtc)
                    .ToList());

        _documentRepositoryMock
            .Setup(repository => repository.SearchDocumentsPageAsync(
                It.IsAny<DocumentQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DocumentQuery query, CancellationToken _) =>
            {
                var normalizedQuery = NormalizeQuery(query);
                var matchingDocuments = ApplySearch(documents, normalizedQuery)
                    .OrderByDescending(document => document.UploadedAtUtc)
                    .ToList();
                var items = matchingDocuments
                    .Skip((normalizedQuery.PageNumber - 1) * normalizedQuery.PageSize)
                    .Take(normalizedQuery.PageSize)
                    .ToList();

                return new PaginatedResult<Document>(
                    items,
                    matchingDocuments.Count,
                    normalizedQuery.PageNumber,
                    normalizedQuery.PageSize);
            });
    }

    private static DocumentQuery NormalizeQuery(DocumentQuery query)
    {
        return query with
        {
            PageNumber = query.PageNumber < 1 ? 1 : query.PageNumber,
            PageSize = query.PageSize < 1
                ? DocumentQuery.DefaultPageSize
                : Math.Min(query.PageSize, DocumentQuery.MaxPageSize)
        };
    }

    private static IEnumerable<Document> ApplySearch(
        IEnumerable<Document> documents,
        DocumentQuery query)
    {
        var searchTerm = string.IsNullOrWhiteSpace(query.SearchTerm)
            ? null
            : query.SearchTerm.Trim();
        var contentType = string.IsNullOrWhiteSpace(query.ContentType)
            ? null
            : query.ContentType.Trim();

        var filteredDocuments = documents;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filteredDocuments = filteredDocuments.Where(document =>
                document.OriginalFileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            filteredDocuments = filteredDocuments.Where(document =>
                string.Equals(document.ContentType, contentType, StringComparison.OrdinalIgnoreCase));
        }

        if (query.UploadedFromUtc.HasValue)
        {
            filteredDocuments = filteredDocuments.Where(document =>
                document.UploadedAtUtc >= query.UploadedFromUtc.Value);
        }

        if (query.UploadedToUtc.HasValue)
        {
            filteredDocuments = filteredDocuments.Where(document =>
                document.UploadedAtUtc <= query.UploadedToUtc.Value);
        }

        return filteredDocuments;
    }
}
