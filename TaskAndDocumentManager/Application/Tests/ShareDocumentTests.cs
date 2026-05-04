using Moq;
using TaskAndDocumentManager.Application.Documents.DTOs;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Domain.Documents;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class ShareDocumentTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly ShareDocument _sut;

    public ShareDocumentTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _sut = new ShareDocument(_documentRepositoryMock.Object, _documentAccessRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGrantAccess_WhenOwnerSharesDocument()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);
        var request = new ShareDocumentRequest
        {
            DocumentId = document.Id,
            TargetUserId = targetUserId,
            GrantedByUserId = ownerId
        };

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _documentAccessRepositoryMock
            .Setup(repository => repository.GrantAccessAsync(It.IsAny<DocumentAccess>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.ExecuteAsync(request);

        _documentAccessRepositoryMock.Verify(repository =>
            repository.GrantAccessAsync(
                It.Is<DocumentAccess>(access =>
                    access.DocumentId == document.Id &&
                    access.UserId == targetUserId &&
                    access.GrantedByUserId == ownerId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenOwnerSharesWithSelf()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);
        var request = new ShareDocumentRequest
        {
            DocumentId = document.Id,
            TargetUserId = ownerId,
            GrantedByUserId = ownerId
        };

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExecuteAsync(request));

        Assert.Equal("You cannot share a document with yourself.", exception.Message);
    }
}
