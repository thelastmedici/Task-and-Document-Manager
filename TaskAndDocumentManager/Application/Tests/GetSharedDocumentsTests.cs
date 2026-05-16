using Moq;
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
        var sharedDocument = new Document("shared.pdf", "application/pdf", 100, "/tmp/shared.pdf", ownerId);
        var otherDocument = new Document("other.pdf", "application/pdf", 100, "/tmp/other.pdf", ownerId);

        _documentAccessRepositoryMock
            .Setup(repository => repository.GetSharedDocumentIdsForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([sharedDocument.Id]);

        _documentRepositoryMock
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([sharedDocument, otherDocument]);

        var result = await _sut.ExecuteAsync(userId, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(sharedDocument.Id, result[0].Id);
        Assert.Equal("shared.pdf", result[0].FileName);
    }
}
