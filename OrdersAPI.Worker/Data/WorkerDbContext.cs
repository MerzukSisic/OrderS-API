using Microsoft.EntityFrameworkCore;
using OrdersAPI.Domain.Entities;

namespace OrdersAPI.Worker.Data;

public class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.Ignore(u => u.Orders);
            entity.Property(u => u.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Ignore(n => n.User);
            entity.Property(n => n.Type).HasConversion<string>();
        });
    }
}
