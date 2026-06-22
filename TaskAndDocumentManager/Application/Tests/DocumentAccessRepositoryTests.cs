using TaskAndDocumentManager.Domain.Documents;
using TaskAndDocumentManager.Infrastructure.Documents;

namespace Tests;

public class DocumentAccessRepositoryTests
{
    [Fact]
    public async Task AccessOperations_ShouldRemainScopedToWorkspace()
    {
        var repository = new DocumentAccessRepository();
        var documentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var grantedByUserId = Guid.NewGuid();
        var firstWorkspaceId = Guid.NewGuid();
        var secondWorkspaceId = Guid.NewGuid();

        await repository.GrantAccessAsync(new DocumentAccess(
            documentId,
            userId,
            grantedByUserId,
            firstWorkspaceId));
        await repository.GrantAccessAsync(new DocumentAccess(
            documentId,
            userId,
            grantedByUserId,
            secondWorkspaceId));

        Assert.True(await repository.HasAccessAsync(documentId, userId, firstWorkspaceId));
        Assert.True(await repository.HasAccessAsync(documentId, userId, secondWorkspaceId));

        await repository.RevokeAccessAsync(documentId, userId, firstWorkspaceId);

        Assert.False(await repository.HasAccessAsync(documentId, userId, firstWorkspaceId));
        Assert.True(await repository.HasAccessAsync(documentId, userId, secondWorkspaceId));

        await repository.RevokeAccessAsync(documentId, userId, secondWorkspaceId);
    }
}
