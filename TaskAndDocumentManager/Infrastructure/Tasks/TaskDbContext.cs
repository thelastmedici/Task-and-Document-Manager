using Microsoft.EntityFrameworkCore;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;
using TaskAndDocumentManager.Domain.Workspaces;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Infrastructure.Tasks;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    // Set this per-request to enforce tenant isolation in query filters
    public Guid CurrentWorkspaceId { get; set; } = Guid.Empty;

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(task => task.Description)
                .HasMaxLength(4000)
                .IsRequired();
            entity.Property(task => task.OwnerId)
                .HasColumnName("CreatedByUserId")
                .IsRequired();
            entity.Property(task => task.WorkspaceId)
                .IsRequired();
            entity.HasIndex(task => task.WorkspaceId);
            entity.Property(task => task.CreatedAt)
                .IsRequired();
            entity.Property(task => task.DueAtUtc);
            entity.Property(task => task.DeadlineReminderSentAtUtc);
            entity.Property(task => task.Priority)
                .IsRequired();
            // Tenant isolation: only include tasks for the current workspace
            entity.HasQueryFilter(task => task.WorkspaceId == CurrentWorkspaceId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(role => role.Id);

            entity.Property(role => role.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(role => role.Name)
                .IsUnique();

            entity.HasData(BuiltInRoles.CreateSeedData());
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);

            entity.Property(user => user.Email)
                .IsRequired();

            entity.Property(user => user.PasswordHash)
                .IsRequired();

            entity.Property(user => user.RoleId)
                .IsRequired();

            entity.HasOne(user => user.Role)
                .WithMany()
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(notification => notification.Id);

            entity.Property(notification => notification.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(notification => notification.Message)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(notification => notification.UserId)
                .IsRequired();

            entity.Property(notification => notification.CreatedAtUtc)
                .IsRequired();

            entity.Property(notification => notification.IsRead)
                .IsRequired();
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(workspace => workspace.Id);

            entity.Property(workspace => workspace.Name)
                .HasMaxLength(Workspace.MaxNameLength)
                .IsRequired();

            entity.Property(workspace => workspace.CreatedAtUtc)
                .IsRequired();

            entity.Property(workspace => workspace.CreatedByUserId)
                .IsRequired();

            entity.HasIndex(workspace => workspace.Name);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(workspace => workspace.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.HasKey(member => new { member.WorkspaceId, member.UserId });

            entity.Property(member => member.Role)
                .HasMaxLength(WorkspaceMember.MaxRoleLength)
                .IsRequired();

            entity.Property(member => member.JoinedAtUtc)
                .IsRequired();

            entity.HasIndex(member => member.UserId);

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(member => member.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tenant isolation: workspace members are scoped to their workspace
            entity.HasQueryFilter(member => member.WorkspaceId == CurrentWorkspaceId);
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(team => team.Id);

            entity.Property(team => team.WorkspaceId)
                .IsRequired();

            entity.Property(team => team.Name)
                .HasMaxLength(Team.MaxNameLength)
                .IsRequired();

            entity.HasIndex(team => new { team.WorkspaceId, team.Name })
                .IsUnique();

            entity.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(team => team.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Tenant isolation: only include teams for the current workspace
            entity.HasQueryFilter(team => team.WorkspaceId == CurrentWorkspaceId);
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(member => new { member.TeamId, member.UserId });

            entity.Property(member => member.JoinedAtUtc)
                .IsRequired();

            entity.HasIndex(member => member.UserId);

            entity.HasOne<Team>()
                .WithMany()
                .HasForeignKey(member => member.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(member => member.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
