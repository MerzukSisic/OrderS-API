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
}
