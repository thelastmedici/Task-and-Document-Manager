using TaskAndDocumentManager.Domain.Workspaces;

namespace Tests;

public class WorkspaceTests
{
    [Fact]
    public void Constructor_ShouldCreateWorkspace()
    {
        var createdByUserId = Guid.NewGuid();

        var workspace = new Workspace("  Acme Ltd  ", createdByUserId);

        Assert.NotEqual(Guid.Empty, workspace.Id);
        Assert.Equal("Acme Ltd", workspace.Name);
        Assert.Equal(createdByUserId, workspace.CreatedByUserId);
        Assert.True(workspace.CreatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyName()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Workspace(" ", Guid.NewGuid()));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyCreatedByUserId()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new Workspace("Acme Ltd", Guid.Empty));

        Assert.Equal("createdByUserId", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldRejectNameLongerThanMaximum()
    {
        var longName = new string('a', Workspace.MaxNameLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            new Workspace(longName, Guid.NewGuid()));

        Assert.Equal("name", exception.ParamName);
    }
}
