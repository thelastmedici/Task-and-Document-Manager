using Microsoft.EntityFrameworkCore;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Tasks;
using TaskAndDocumentManager.Domain.Workspaces;
using TaskAndDocumentManager.Infrastructure.Persistence;

namespace TaskAndDocumentManager.Infrastructure.Tasks;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();

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
            entity.Property(task => task.CreatedAt)
                .IsRequired();
            entity.Property(task => task.DueAtUtc);
            entity.Property(task => task.DeadlineReminderSentAtUtc);
            entity.Property(task => task.Priority)
                .IsRequired();
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
    }
}
