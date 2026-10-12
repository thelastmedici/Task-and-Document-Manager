public class EfWorkspaceRepository : IWorkspaceRepository
{
    private readonly AppDbContext _db;
    public EfWorkspaceRepository(AppDbContext db) => _db = db;

    public Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Workspaces.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task AddAsync(Workspace workspace, CancellationToken ct = default)
    {
        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync(ct);
    }
}