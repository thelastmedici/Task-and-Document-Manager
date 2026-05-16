using Moq;
using TaskAndDocumentManager.Application.Documents.Interfaces;
using TaskAndDocumentManager.Application.Documents.UseCases;
using TaskAndDocumentManager.Domain.Entities;

namespace TaskAndDocumentManager.Application.Tests.Documents.UseCases;

public class RevokeDocumentAccessTests
{
    private readonly Mock<IDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IDocumentAccessRepository> _documentAccessRepositoryMock;
    private readonly RevokeDocumentAccess _sut;

    public RevokeDocumentAccessTests()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _documentAccessRepositoryMock = new Mock<IDocumentAccessRepository>();
        _sut = new RevokeDocumentAccess(_documentRepositoryMock.Object, _documentAccessRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRevokeAccess_WhenOwnerRequestsRevocation()
    {
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        await _sut.ExecuteAsync(document.Id, targetUserId, ownerId, false, CancellationToken.None);

        _documentAccessRepositoryMock.Verify(
            repository => repository.RevokeAccessAsync(document.Id, targetUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenNonOwnerNonAdminAttemptsRevocation()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _sut.ExecuteAsync(document.Id, targetUserId, otherUserId, false, CancellationToken.None));

        Assert.Equal("Only the owner can revoke document access.", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrow_WhenTryingToRevokeOwnerAccess()
    {
        var ownerId = Guid.NewGuid();
        var document = new Document("report.pdf", "application/pdf", 2048, "/tmp/report.pdf", ownerId);

        _documentRepositoryMock
            .Setup(repository => repository.GetByIdAsync(document.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ExecuteAsync(document.Id, ownerId, ownerId, false, CancellationToken.None));

        Assert.Equal("Owner access cannot be revoked.", exception.Message);
    }
}
