using Microsoft.EntityFrameworkCore;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Infrastructure.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        context.Database.EnsureCreated();

        // Ensure token tables exist on existing databases (EnsureCreated is no-op when DB already exists).
        // These guards are safe to run on every startup; they only create if absent.
        EnsureTokenTablesExist(context);
        EnsureSoftDeleteColumnsExist(context);
        EnsureOrderArchiveColumnsExist(context);
        EnsureOrderNumberColumnExists(context);
        EnsureDecimalInventoryColumnsExist(context);
        EnsureProcurementPaymentAndReceiveColumnsExist(context);
        EnsureStatusOptionsTableExists(context);
        SeedStatusOptions(context);

        // Proveri da li već postoje podaci
        if (context.Users.Any())
        {
            return; // DB već ima podatke
        }

        // Categories
        var categories = new[]
        {
            new Category { Id = Guid.NewGuid(), Name = "Piće", IconName = "local_cafe" },
            new Category { Id = Guid.NewGuid(), Name = "Hrana", IconName = "restaurant" },
            new Category { Id = Guid.NewGuid(), Name = "Desert", IconName = "cake" }
        };
        context.Categories.AddRange(categories);
        context.SaveChanges();

        // Products
        var products = new[]
        {
            new Product 
            { 
                Id = Guid.NewGuid(), 
                Name = "Espresso", 
                Price = 1.50m, 
                CategoryId = categories[0].Id,
                Location = PreparationLocation.Bar,
                Stock = 100
            },
            new Product 
            { 
                Id = Guid.NewGuid(), 
                Name ="Club Sendvič", 
                Price = 5.50m, 
                CategoryId = categories[1].Id,
                Location = PreparationLocation.Kitchen,
                Stock = 50
            },
            new Product 
            { 
                Id = Guid.NewGuid(), 
                Name = "Tiramisu", 
                Price = 4.00m, 
                CategoryId = categories[2].Id,
                Location = PreparationLocation.Bar,
                Stock = 20
            }
        };
        context.Products.AddRange(products);
        context.SaveChanges();

        // Users (password: "password123")
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("password123");
        var users = new[]
        {
            new User 
            { 
                Id = Guid.NewGuid(), 
                FullName = "Admin User", 
                Email = "admin@orders.com",
                PasswordHash = passwordHash,
                Role = UserRole.Admin
            },
            new User 
            { 
                Id = Guid.NewGuid(), 
                FullName = "Marko Marković", 
                Email = "marko@orders.com",
                PasswordHash = passwordHash,
                Role = UserRole.Waiter
            },
            new User 
            { 
                Id = Guid.NewGuid(), 
                FullName = "Ana Anić", 
                Email = "ana@orders.com",
                PasswordHash = passwordHash,
                Role = UserRole.Bartender
            },
            new User 
            { 
                Id = Guid.NewGuid(), 
                FullName = "Kuhar Jedan", 
                Email = "kuhar@orders.com",
                PasswordHash = passwordHash,
                Role = UserRole.Kitchen
            },
        };
        context.Users.AddRange(users);
        context.SaveChanges();

        // Tables
        var tables = new[]
        {
            new CafeTable { Id = Guid.NewGuid(), TableNumber ="1", Capacity = 4, Location = "Unutra" },
            new CafeTable { Id = Guid.NewGuid(), TableNumber = "2", Capacity = 2, Location = "Terasa" }
        };
        context.CafeTables.AddRange(tables);
        context.SaveChanges();

        // Store
        var store = new Store { Id = Guid.NewGuid(), Name = "Glavni Magacin" };
        context.Stores.Add(store);
        context.SaveChanges();

        // StoreProducts (sastojci)
        var storeProducts = new[]
        {
            new StoreProduct 
            { 
                Id = Guid.NewGuid(), 
                StoreId = store.Id, 
                Name = "Kafa (zrna)",
                PurchasePrice = 15.00m,
                CurrentStock = 100,
                MinimumStock = 20,
                Unit = "kg"
            },
            new StoreProduct 
            { 
                Id = Guid.NewGuid(), 
                StoreId = store.Id, 
                Name = "Mlijeko",
                PurchasePrice = 1.50m,
                CurrentStock = 50,
                MinimumStock = 10,
                Unit = "l"
            },
            new StoreProduct 
            { 
                Id = Guid.NewGuid(), 
                StoreId = store.Id, 
                Name = "Šunka",
                PurchasePrice = 8.00m,
                CurrentStock = 30,
                MinimumStock = 5,
                Unit = "kg"
            }
        };
        context.StoreProducts.AddRange(storeProducts);
        context.SaveChanges();

        // ProductIngredients
        var productIngredients = new[]
        {
            new ProductIngredient 
            { 
                Id = Guid.NewGuid(), 
                ProductId = products[0].Id, 
                StoreProductId = storeProducts[0].Id,
                Quantity = 0.02m // 20g kafe za espresso
            },
            new ProductIngredient 
            { 
                Id = Guid.NewGuid(), 
                ProductId = products[1].Id, 
                StoreProductId = storeProducts[2].Id,
                Quantity = 0.1m // 100g šunke za sendvič
            }
        };
        context.ProductIngredients.AddRange(productIngredients);
        context.SaveChanges();
    }

    /// <summary>
    /// Creates RefreshTokens and PasswordResetTokens tables when they are missing from an
    /// existing database. EnsureCreated() only runs on brand-new databases; this guard
    /// handles the incremental case so deployments never break at runtime.
    /// Safe to call every startup — IF NOT EXISTS makes it idempotent.
    /// Not called for InMemory test databases (those go through EnsureCreated only).
    /// </summary>
    private static void EnsureTokenTablesExist(ApplicationDbContext context)
    {
        // Skip for InMemory provider — it doesn't support raw SQL
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return;

        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefreshTokens')
            BEGIN
                CREATE TABLE RefreshTokens (
                    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    UserId      UNIQUEIDENTIFIER NOT NULL,
                    TokenHash   NVARCHAR(512)    NOT NULL,
                    ExpiresAt   DATETIME2        NOT NULL,
                    IsRevoked   BIT              NOT NULL DEFAULT 0,
                    CreatedAt   DATETIME2        NOT NULL,
                    RevokedAt   DATETIME2        NULL,
                    CONSTRAINT FK_RefreshTokens_Users
                        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_RefreshTokens_TokenHash ON RefreshTokens(TokenHash);
                CREATE INDEX IX_RefreshTokens_UserId    ON RefreshTokens(UserId);
            END");

        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PasswordResetTokens')
            BEGIN
                CREATE TABLE PasswordResetTokens (
                    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    UserId      UNIQUEIDENTIFIER NOT NULL,
                    TokenHash   NVARCHAR(512)    NOT NULL,
                    ExpiresAt   DATETIME2        NOT NULL,
                    IsUsed      BIT              NOT NULL DEFAULT 0,
                    CreatedAt   DATETIME2        NOT NULL,
                    CONSTRAINT FK_PasswordResetTokens_Users
                        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_PasswordResetTokens_TokenHash ON PasswordResetTokens(TokenHash);
                CREATE INDEX IX_PasswordResetTokens_UserId    ON PasswordResetTokens(UserId);
            END");
    }

    private static void EnsureSoftDeleteColumnsExist(ApplicationDbContext context)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return;

        context.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('Products', 'IsDeleted') IS NULL
            BEGIN
                ALTER TABLE Products ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT 0;
                CREATE INDEX IX_Products_IsDeleted ON Products(IsDeleted);
            END");

        context.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('Categories', 'IsDeleted') IS NULL
            BEGIN
                ALTER TABLE Categories ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Categories_IsDeleted DEFAULT 0;
                CREATE INDEX IX_Categories_IsDeleted ON Categories(IsDeleted);
            END");

        context.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('Stores', 'IsDeleted') IS NULL
            BEGIN
                ALTER TABLE Stores ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Stores_IsDeleted DEFAULT 0;
                CREATE INDEX IX_Stores_IsDeleted ON Stores(IsDeleted);
            END");
    }

    private static void EnsureOrderArchiveColumnsExist(ApplicationDbContext context)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return;

        context.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('Orders', 'IsArchived') IS NULL
            BEGIN
                ALTER TABLE Orders ADD IsArchived BIT NOT NULL CONSTRAINT DF_Orders_IsArchived DEFAULT 0;
                CREATE INDEX IX_Orders_IsArchived ON Orders(IsArchived);
            END");

        context.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('Orders', 'ArchivedAt') IS NULL
            BEGIN
                ALTER TABLE Orders ADD ArchivedAt DATETIME2 NULL;
            END");
    }

    private static void EnsureOrderNumberColumnExists(ApplicationDbContext context)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return;

        context.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('Orders', 'OrderNumber') IS NULL
            BEGIN
                ALTER TABLE Orders ADD OrderNumber INT NOT NULL IDENTITY(1,1);
                CREATE UNIQUE INDEX IX_Orders_OrderNumber ON Orders(OrderNumber);
            END");
    }

    private static void EnsureDecimalInventoryColumnsExist(ApplicationDbContext context)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return;

        // StoreProducts.CurrentStock: int -> decimal(18,4)
        context.Database.ExecuteSqlRaw(@"
            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'StoreProducts' AND COLUMN_NAME = 'CurrentStock' AND DATA_TYPE = 'int'
            )
            BEGIN
                ALTER TABLE StoreProducts ALTER COLUMN CurrentStock DECIMAL(18,4) NOT NULL;
            END");

        // StoreProducts.MinimumStock: int -> decimal(18,4)
        context.Database.ExecuteSqlRaw(@"
            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'StoreProducts' AND COLUMN_NAME = 'MinimumStock' AND DATA_TYPE = 'int'
            )
            BEGIN
                ALTER TABLE StoreProducts ALTER COLUMN MinimumStock DECIMAL(18,4) NOT NULL;
            END");

        // InventoryLogs.QuantityChange: int -> decimal(18,4)
        context.Database.ExecuteSqlRaw(@"
            IF EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'InventoryLogs' AND COLUMN_NAME = 'QuantityChange' AND DATA_TYPE = 'int'
            )
            BEGIN
                ALTER TABLE InventoryLogs ALTER COLUMN QuantityChange DECIMAL(18,4) NOT NULL;
            END");
    }

    private static void EnsureStatusOptionsTableExists(ApplicationDbContext context)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return;

        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StatusOptions')
            BEGIN
                CREATE TABLE StatusOptions (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Category NVARCHAR(50) NOT NULL,
                    Name NVARCHAR(50) NOT NULL,
                    DisplayName NVARCHAR(100) NOT NULL,
                    Description NVARCHAR(500) NULL,
                    SortOrder INT NOT NULL DEFAULT 0,
                    IsActive BIT NOT NULL DEFAULT 1,
                    CONSTRAINT UQ_StatusOptions_Category_Name UNIQUE (Category, Name)
                );
            END");
    }

    private static void EnsureProcurementPaymentAndReceiveColumnsExist(ApplicationDbContext context)
    {
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return;

        context.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('ProcurementOrders', 'StripeCheckoutSessionId') IS NULL
            BEGIN
                ALTER TABLE ProcurementOrders ADD StripeCheckoutSessionId NVARCHAR(200) NULL;
            END");

        context.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH('ProcurementOrderItems', 'ReceivedQuantity') IS NULL
            BEGIN
                ALTER TABLE ProcurementOrderItems ADD ReceivedQuantity INT NOT NULL CONSTRAINT DF_ProcurementOrderItems_ReceivedQuantity DEFAULT 0;
            END");
    }

    private static void SeedStatusOptions(ApplicationDbContext context)
    {
        if (context.StatusOptions.Any())
            return;

        var options = new List<StatusOption>
        {
            // Order statuses
            new() { Category = "OrderStatus", Name = "Pending",   DisplayName = "Pending",   Description = "Order received, waiting to be prepared", SortOrder = 1 },
            new() { Category = "OrderStatus", Name = "Preparing", DisplayName = "Preparing", Description = "Order is currently being prepared",       SortOrder = 2 },
            new() { Category = "OrderStatus", Name = "Ready",     DisplayName = "Ready",     Description = "Order is ready for serving",             SortOrder = 3 },
            new() { Category = "OrderStatus", Name = "Completed", DisplayName = "Completed", Description = "Order has been served and completed",     SortOrder = 4 },
            new() { Category = "OrderStatus", Name = "Cancelled", DisplayName = "Cancelled", Description = "Order has been cancelled",               SortOrder = 5 },

            // OrderItem statuses
            new() { Category = "OrderItemStatus", Name = "Pending",   DisplayName = "Pending",   Description = "Item waiting to be prepared", SortOrder = 1 },
            new() { Category = "OrderItemStatus", Name = "Preparing", DisplayName = "Preparing", Description = "Item is being prepared",      SortOrder = 2 },
            new() { Category = "OrderItemStatus", Name = "Ready",     DisplayName = "Ready",     Description = "Item is ready",               SortOrder = 3 },
            new() { Category = "OrderItemStatus", Name = "Completed", DisplayName = "Completed", Description = "Item delivered",              SortOrder = 4 },
            new() { Category = "OrderItemStatus", Name = "Cancelled", DisplayName = "Cancelled", Description = "Item cancelled",              SortOrder = 5 },

            // Procurement statuses
            new() { Category = "ProcurementStatus", Name = "Pending",   DisplayName = "Pending",   Description = "Procurement order created, awaiting payment", SortOrder = 1 },
            new() { Category = "ProcurementStatus", Name = "Paid",      DisplayName = "Paid",      Description = "Payment confirmed via Stripe",                 SortOrder = 2 },
            new() { Category = "ProcurementStatus", Name = "Ordered",   DisplayName = "Ordered",   Description = "Order placed with supplier",                   SortOrder = 3 },
            new() { Category = "ProcurementStatus", Name = "PartiallyReceived", DisplayName = "Partially received", Description = "Some ordered goods were received", SortOrder = 4 },
            new() { Category = "ProcurementStatus", Name = "Received",  DisplayName = "Received",  Description = "Goods received and inventory updated",         SortOrder = 5 },
            new() { Category = "ProcurementStatus", Name = "Cancelled", DisplayName = "Cancelled", Description = "Procurement order cancelled",                  SortOrder = 6 },

            // Table statuses
            new() { Category = "TableStatus", Name = "Available", DisplayName = "Available", Description = "Table is free",     SortOrder = 1 },
            new() { Category = "TableStatus", Name = "Occupied",  DisplayName = "Occupied",  Description = "Table has guests",  SortOrder = 2 },
            new() { Category = "TableStatus", Name = "Reserved",  DisplayName = "Reserved",  Description = "Table is reserved", SortOrder = 3 },

            // Inventory log types
            new() { Category = "InventoryLogType", Name = "Sale",        DisplayName = "Sale",        Description = "Stock deducted due to sale",         SortOrder = 1 },
            new() { Category = "InventoryLogType", Name = "Restock",     DisplayName = "Restock",     Description = "Stock added via procurement",        SortOrder = 2 },
            new() { Category = "InventoryLogType", Name = "Adjustment",  DisplayName = "Adjustment",  Description = "Manual stock adjustment",            SortOrder = 3 },
            new() { Category = "InventoryLogType", Name = "Damage",      DisplayName = "Damage",      Description = "Stock written off due to damage",    SortOrder = 4 },
            new() { Category = "InventoryLogType", Name = "Addition",    DisplayName = "Addition",    Description = "Stock manually added",               SortOrder = 5 },
            new() { Category = "InventoryLogType", Name = "Subtraction", DisplayName = "Subtraction", Description = "Stock manually subtracted",          SortOrder = 6 },
        };

        context.StatusOptions.AddRange(options);
        context.SaveChanges();
    }
}
