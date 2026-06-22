using Moq;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class GetSharedDocumentsTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly GetSharedDocuments _sut;

    public GetSharedDocumentsTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _sut = new GetSharedDocuments(_documentRepositoryMock.Object, _documentAccessRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOnlyDocumentsSharedWithUser()
    {
        var userId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var sharedDocument = new Document(
            "shared.pdf",
            "application/pdf",
            100,
            "/tmp/shared.pdf",
            ownerId,
            workspaceId);
        var otherDocument = new Document(
            "other.pdf",
            "application/pdf",
            100,
            "/tmp/other.pdf",
            ownerId,
            workspaceId);

        _documentAccessRepositoryMock
            .Setup(repository => repository.GetSharedDocumentIdsForUserAsync(
                userId,
                workspaceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([sharedDocument.Id]);

        _documentRepositoryMock
            .Setup(repository => repository.SearchDocumentsAsync(
                It.IsAny<DocumentQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([sharedDocument, otherDocument]);

        var result = await _sut.ExecuteAsync(userId, workspaceId, cancellationToken: CancellationToken.None);

        var document = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(sharedDocument.Id, document.Id);
        Assert.Equal("shared.pdf", document.FileName);
    }
}
