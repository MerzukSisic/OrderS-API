using Microsoft.EntityFrameworkCore;
using OrdersAPI.Domain.Entities;

namespace OrdersAPI.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CafeTable> CafeTables => Set<CafeTable>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreProduct> StoreProducts => Set<StoreProduct>();
    public DbSet<ProductIngredient> ProductIngredients => Set<ProductIngredient>();
    public DbSet<ProcurementOrder> ProcurementOrders => Set<ProcurementOrder>();
    public DbSet<ProcurementOrderItem> ProcurementOrderItems => Set<ProcurementOrderItem>();
    public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AccompanimentGroup> AccompanimentGroups => Set<AccompanimentGroup>();
    public DbSet<Accompaniment> Accompaniments => Set<Accompaniment>();
    public DbSet<OrderItemAccompaniment> OrderItemAccompaniments => Set<OrderItemAccompaniment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Role).HasConversion<string>();
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Location).HasConversion<string>();
            
            entity.HasOne(e => e.Category)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.HasOne(e => e.Waiter)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.WaiterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Table)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.TableId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);

            entity.HasOne(e => e.Order)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(e => e.OrderItems)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CafeTable
        modelBuilder.Entity<CafeTable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
        });

        // Store
        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        // StoreProduct
        modelBuilder.Entity<StoreProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);

            entity.HasOne(e => e.Store)
                .WithMany(e => e.StoreProducts)
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductIngredient
        modelBuilder.Entity<ProductIngredient>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).HasPrecision(18, 2);

            entity.HasOne(e => e.Product)
                .WithMany(e => e.ProductIngredients)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.StoreProduct)
                .WithMany(e => e.ProductIngredients)
                .HasForeignKey(e => e.StoreProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProcurementOrder
        modelBuilder.Entity<ProcurementOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.HasOne(e => e.Store)
                .WithMany()
                .HasForeignKey(e => e.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProcurementOrderItem
        modelBuilder.Entity<ProcurementOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);

            entity.HasOne(e => e.ProcurementOrder)
                .WithMany(e => e.Items)
                .HasForeignKey(e => e.ProcurementOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.StoreProduct)
                .WithMany()
                .HasForeignKey(e => e.StoreProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // InventoryLog
        modelBuilder.Entity<InventoryLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>();

            entity.HasOne(e => e.StoreProduct)
                .WithMany()
                .HasForeignKey(e => e.StoreProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<string>();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AccompanimentGroup
        modelBuilder.Entity<AccompanimentGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SelectionType).HasConversion<string>();

            entity.HasOne(e => e.Product)
                .WithMany(e => e.AccompanimentGroups)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Accompaniment
        modelBuilder.Entity<Accompaniment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ExtraCharge).HasPrecision(18, 2);

            entity.HasOne(e => e.AccompanimentGroup)
                .WithMany(e => e.Accompaniments)
                .HasForeignKey(e => e.AccompanimentGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItemAccompaniment
        modelBuilder.Entity<OrderItemAccompaniment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PriceAtOrder).HasPrecision(18, 2);

            entity.HasOne(e => e.OrderItem)
                .WithMany(e => e.OrderItemAccompaniments)
                .HasForeignKey(e => e.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Accompaniment)
                .WithMany(e => e.OrderItemAccompaniments)
                .HasForeignKey(e => e.AccompanimentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}