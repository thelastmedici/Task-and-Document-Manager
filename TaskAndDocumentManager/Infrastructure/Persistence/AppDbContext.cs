using System.Dynamic;
using Microsoft.EntityFrameworkCore;
using TaskAndDocumentManager.Domain.Auth;
using TaskAndDocumentManager.Domain.Documents;
using TaskAndDocumentManager.Domain.Entities;
using TaskAndDocumentManager.Domain.Workspaces;

namespace TaskAndDocumentManager.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<User> Users => Set<User>();
    public Dbset<Role> Roles => Set<Roles>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<DocumentAccess> DocumentAccesses => Set<DocumentAccess>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Document>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Id).ValueGeneratedNever();
            e.Property(d => d.Version).IsRowVersion();
            e.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => d.WorkspaceId);
        });

        b.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedNever();
            e.Property(u => u.Email).IsRequired().HasMaxLength(320);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
        });

        b.Entity<DocumentAccess>(e =>
        {
            e.HasKey(access => access.Id);
            e.Property(access => access.Id).ValueGeneratedNever();
            e.HasOne<Document>()
                .WithMany()
                .HasForeignKey(access => access.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(access => access.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(access => access.GrantedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Workspace>()
                .WithMany()
                .HasForeignKey(access => access.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(access => new { access.DocumentId, access.UserId }).IsUnique();
        });

        b.Entity<AuditLog>(e =>
        {
            e.HasKey(auditLog => auditLog.Id);
            e.Property(auditLog => auditLog.Id).ValueGeneratedNever();
            e.HasIndex(auditLog => auditLog.CreatedAtUtc);
            e.HasIndex(auditLog => new { auditLog.WorkspaceId, auditLog.CreatedAtUtc });
        });

        b.Entity<Workspace>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Id).ValueGeneratedNever();
            e.Property(w => w.Name).IsRequired().HasMaxLength(Workspace.MaxNameLength);
            e.HasIndex(w => w.CreatedByUserId);
        });

        b.Entity<WorkspaceMember>(e =>
        {
            e.HasKey(m => new { m.WorkspaceId, m.UserId });
            e.Property(m => m.Role).IsRequired().HasMaxLength(WorkspaceMember.MaxRoleLength);
            e.HasOne<Workspace>().WithMany().HasForeignKey(m => m.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(m => m.UserId);
        });

        b.Entity<Team>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedNever();
            e.Property(t => t.Name).IsRequired().HasMaxLength(Team.MaxNameLength);
            e.HasOne<Workspace>().WithMany().HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(t => new { t.WorkspaceId, t.Name }).IsUnique();
        });

        b.Entity<PasswordResetToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedNever();
            e.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
            e.HasIndex(t => t.TokenHash).IsUnique();
            e.HasIndex(t => t.ExpiresAtUtc);
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
