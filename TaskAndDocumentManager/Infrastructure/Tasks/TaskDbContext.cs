using Microsoft.EntityFrameworkCore;
using TaskAndDocumentManager.Domain.Tasks;

namespace TaskAndDocumentManager.Infrastructure.Tasks;

public class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

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
            entity.Property(task => task.CreatedAt)
                .IsRequired();
        });
    }
}
