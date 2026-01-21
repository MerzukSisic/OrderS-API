using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class ProductService(
    ApplicationDbContext context,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(Guid? categoryId = null, bool? isAvailable = null)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (isAvailable.HasValue)
            query = query.Where(p => p.IsAvailable == isAvailable.Value);

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                ImageUrl = p.ImageUrl,
                IsAvailable = p.IsAvailable,
                PreparationLocation = p.Location.ToString(),
                PreparationTimeMinutes = p.PreparationTimeMinutes,
                Stock = p.Stock,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Ingredients = p.ProductIngredients.Select(pi => new ProductIngredientDto
                {
                    Id = pi.Id,
                    StoreProductId = pi.StoreProductId,
                    StoreProductName = pi.StoreProduct.Name,
                    Quantity = pi.Quantity,
                    Unit = pi.StoreProduct.Unit.ToString()
                }).ToList(),
                AccompanimentGroups = new List<AccompanimentGroupDto>() // Populate if needed
            })
            .ToListAsync();

        return products;
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .Include(p => p.AccompanimentGroups)
                .ThenInclude(ag => ag.Accompaniments)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                ImageUrl = p.ImageUrl,
                IsAvailable = p.IsAvailable,
                PreparationLocation = p.Location.ToString(),
                PreparationTimeMinutes = p.PreparationTimeMinutes,
                Stock = p.Stock,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Ingredients = p.ProductIngredients.Select(pi => new ProductIngredientDto
                {
                    Id = pi.Id,
                    StoreProductId = pi.StoreProductId,
                    StoreProductName = pi.StoreProduct.Name,
                    Quantity = pi.Quantity,
                    Unit = pi.StoreProduct.Unit.ToString()
                }).ToList(),
                AccompanimentGroups = p.AccompanimentGroups.Select(ag => new AccompanimentGroupDto
                {
                    Id = ag.Id,
                    Name = ag.Name,
                    ProductId = ag.ProductId,
                    SelectionType = ag.SelectionType.ToString(),
                    IsRequired = ag.IsRequired,
                    MinSelections = ag.MinSelections,
                    MaxSelections = ag.MaxSelections,
                    DisplayOrder = ag.DisplayOrder,
                    Accompaniments = ag.Accompaniments.Select(a => new AccompanimentDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        ExtraCharge = a.ExtraCharge,
                        AccompanimentGroupId = a.AccompanimentGroupId,
                        DisplayOrder = a.DisplayOrder,
                        IsAvailable = a.IsAvailable
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found");

        return product;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        var categoryExists = await context.Categories.AnyAsync(c => c.Id == dto.CategoryId);
        if (!categoryExists)
            throw new KeyNotFoundException($"Category with ID {dto.CategoryId} not found");

        // Validate all ingredients exist
        var ingredientIds = dto.Ingredients.Select(i => i.StoreProductId).ToList();
        var existingIngredients = await context.StoreProducts
            .Where(sp => ingredientIds.Contains(sp.Id))
            .Select(sp => sp.Id)
            .ToListAsync();

        var missingIngredients = ingredientIds.Except(existingIngredients).ToList();
        if (missingIngredients.Any())
            throw new KeyNotFoundException($"Store products not found: {string.Join(", ", missingIngredients)}");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            ImageUrl = dto.ImageUrl,
            Location = Enum.Parse<PreparationLocation>(dto.PreparationLocation),
            PreparationTimeMinutes = dto.PreparationTimeMinutes,
            Stock = dto.Stock,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Products.Add(product);

        foreach (var ingredientDto in dto.Ingredients)
        {
            var ingredient = new ProductIngredient
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                StoreProductId = ingredientDto.StoreProductId,
                Quantity = ingredientDto.Quantity
            };
            context.ProductIngredients.Add(ingredient);
        }

        await context.SaveChangesAsync();
        
        logger.LogInformation("Product {ProductId} created: {ProductName}", product.Id, product.Name);

        return await GetProductByIdAsync(product.Id);
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductDto dto)
    {
        var product = await context.Products.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found");

        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await context.Categories.AnyAsync(c => c.Id == dto.CategoryId.Value);
            if (!categoryExists)
                throw new KeyNotFoundException($"Category with ID {dto.CategoryId} not found");
        }

        if (dto.Name != null) product.Name = dto.Name;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId.Value;
        if (dto.ImageUrl != null) product.ImageUrl = dto.ImageUrl;
        if (dto.IsAvailable.HasValue) product.IsAvailable = dto.IsAvailable.Value;
        if (dto.PreparationLocation != null) 
            product.Location = Enum.Parse<PreparationLocation>(dto.PreparationLocation);
        if (dto.PreparationTimeMinutes.HasValue) 
            product.PreparationTimeMinutes = dto.PreparationTimeMinutes.Value;
        if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;

        product.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        
        logger.LogInformation("Product {ProductId} updated", id);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await context.Products
            .Include(p => p.ProductIngredients)
            .Include(p => p.AccompanimentGroups)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            throw new KeyNotFoundException($"Product with ID {id} not found");

        // Check if product is used in any active orders
        var hasActiveOrders = await context.OrderItems
            .AnyAsync(oi => oi.ProductId == id && 
                (oi.Order.Status == OrderStatus.Pending || oi.Order.Status == OrderStatus.Preparing));

        if (hasActiveOrders)
            throw new InvalidOperationException("Cannot delete product with active orders");

        context.Products.Remove(product);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Product {ProductId} deleted", id);
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return await GetAllProductsAsync();

        var products = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .Where(p => EF.Functions.Like(p.Name, $"%{searchTerm}%") || 
                       (p.Description != null && EF.Functions.Like(p.Description, $"%{searchTerm}%")))
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                ImageUrl = p.ImageUrl,
                IsAvailable = p.IsAvailable,
                PreparationLocation = p.Location.ToString(),
                PreparationTimeMinutes = p.PreparationTimeMinutes,
                Stock = p.Stock,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Ingredients = p.ProductIngredients.Select(pi => new ProductIngredientDto
                {
                    Id = pi.Id,
                    StoreProductId = pi.StoreProductId,
                    StoreProductName = pi.StoreProduct.Name,
                    Quantity = pi.Quantity,
                    Unit = pi.StoreProduct.Unit.ToString()
                }).ToList(),
                AccompanimentGroups = new List<AccompanimentGroupDto>()
            })
            .ToListAsync();

        return products;
    }

    public async Task<bool> ToggleAvailabilityAsync(Guid productId)
    {
        var product = await context.Products.FindAsync(productId);
        if (product == null)
            throw new KeyNotFoundException($"Product with ID {productId} not found");

        product.IsAvailable = !product.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("Product {ProductId} availability toggled to {IsAvailable}", 
            productId, product.IsAvailable);

        return product.IsAvailable;
    }

    public async Task<List<ProductDto>> GetProductsByLocationAsync(PreparationLocation location, bool? isAvailable = null)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .Where(p => p.Location == location)
            .AsQueryable();

        if (isAvailable.HasValue)
            query = query.Where(p => p.IsAvailable == isAvailable.Value);

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                ImageUrl = p.ImageUrl,
                IsAvailable = p.IsAvailable,
                PreparationLocation = p.Location.ToString(),
                PreparationTimeMinutes = p.PreparationTimeMinutes,
                Stock = p.Stock,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Ingredients = p.ProductIngredients.Select(pi => new ProductIngredientDto
                {
                    Id = pi.Id,
                    StoreProductId = pi.StoreProductId,
                    StoreProductName = pi.StoreProduct.Name,
                    Quantity = pi.Quantity,
                    Unit = pi.StoreProduct.Unit.ToString()
                }).ToList(),
                AccompanimentGroups = new List<AccompanimentGroupDto>()
            })
            .ToListAsync();

        return products;
    }

    public async Task BulkUpdateAvailabilityAsync(List<Guid> productIds, bool isAvailable)
    {
        var updatedCount = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ExecuteUpdateAsync(p => p
                .SetProperty(x => x.IsAvailable, isAvailable)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow));

        logger.LogInformation("Bulk updated availability for {Count} products to {IsAvailable}", 
            updatedCount, isAvailable);
    }
}
