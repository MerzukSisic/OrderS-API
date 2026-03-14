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

        modelBuilder.Entity<User>()
            .Ignore(u => u.Orders);

        modelBuilder.Entity<Notification>()
            .Ignore(n => n.User);
    }
}
