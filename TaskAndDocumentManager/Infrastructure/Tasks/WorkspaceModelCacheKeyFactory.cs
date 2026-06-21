using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace TaskAndDocumentManager.Infrastructure.Tasks;

public class WorkspaceModelCacheKeyFactory : IModelCacheKeyFactory
{
    // EF Core changed the IModelCacheKeyFactory signature in newer SDKs; implement both overloads
    public object Create(DbContext context)
    {
        return CreateCore(context);
    }

    public object Create(DbContext context, bool designTime)
    {
        return CreateCore(context);
    }

    private static object CreateCore(DbContext context)
    {
        if (context is TaskDbContext taskDbContext)
        {
            // Include the current workspace id in the model cache key so that
            // EF rebuilds the model when the workspace changes. This allows
            // query filters that reference the context's CurrentWorkspaceId
            // to behave correctly per-tenant.
            return (context.GetType(), taskDbContext.CurrentWorkspaceId);
        }

        return context.GetType();
    }
}
