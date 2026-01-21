using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Infrastructure.Data;

public static class DbInitializer
{
    public static void Initialize(ApplicationDbContext context)
    {
        context.Database.EnsureCreated();

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
            }
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
}
