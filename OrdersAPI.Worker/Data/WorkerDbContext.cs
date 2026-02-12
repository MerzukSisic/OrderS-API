using Microsoft.EntityFrameworkCore;

namespace OrdersAPI.Worker.Data;

public class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
{
    // Add DbSets here if Worker needs to access database
    // For example, if you need to read/write logs, notifications, etc.
    
    // Example:
    // public DbSet<WorkerLog> WorkerLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure entities here if needed
    }
}
