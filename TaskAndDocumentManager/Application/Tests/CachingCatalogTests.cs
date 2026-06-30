using Microsoft.Extensions.Caching.Memory;
using TaskAndDocumentManager.Infrastructure.Documents;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Application.Tests.Infrastructure;

public class CachingCatalogTests
{
    [Fact]
    public void RoleCatalog_ShouldResolveBuiltInRoles_FromMemoryCache()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new RoleCatalog(cache);

        Assert.True(sut.IsSupportedRole(sut.AdminRoleId));
        Assert.Equal(BuiltInRoles.AdminName, sut.ResolveName(sut.AdminRoleId));
        Assert.Equal(BuiltInRoles.ManagerName, sut.ResolveName(sut.ManagerRoleId));
        Assert.Equal(BuiltInRoles.UserName, sut.ResolveName(Guid.NewGuid()));
    }

    [Fact]
    public void AllowedDocumentTypeCatalog_ShouldCacheStableAllowedTypes()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new AllowedDocumentTypeCatalog(cache);

        var firstRead = sut.GetAllowedTypes();
        var secondRead = sut.GetAllowedTypes();

        Assert.Same(firstRead, secondRead);
        Assert.True(sut.IsAllowedExtension(".pdf"));
        Assert.True(sut.IsAllowedContentType(".PDF", "APPLICATION/PDF"));
        Assert.False(sut.IsAllowedExtension(".exe"));
        Assert.False(sut.IsAllowedContentType(".pdf", "application/x-msdownload"));
    }
}
