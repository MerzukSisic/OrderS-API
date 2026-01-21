using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class InventoryService(ApplicationDbContext context, ILogger<InventoryService> logger)
    : IInventoryService
{
    public async Task<IEnumerable<StoreProductDto>> GetAllStoreProductsAsync(Guid? storeId = null)
    {
        var query = context.StoreProducts
            .AsNoTracking()
            .Include(sp => sp.Store)
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(sp => sp.StoreId == storeId);

        var products = await query
            .Select(sp => new StoreProductDto
            {
                Id = sp.Id,
                StoreId = sp.StoreId,
                StoreName = sp.Store.Name,
                Name = sp.Name,
                Description = sp.Description,
                PurchasePrice = sp.PurchasePrice,
                CurrentStock = sp.CurrentStock,
                MinimumStock = sp.MinimumStock,
                Unit = sp.Unit,
                IsLowStock = sp.CurrentStock < sp.MinimumStock,
                LastRestocked = sp.LastRestocked,
                CreatedAt = sp.CreatedAt
            })
            .ToListAsync();

        return products;
    }

    public async Task<StoreProductDto> GetStoreProductByIdAsync(Guid id)
    {
        var product = await context.StoreProducts
            .AsNoTracking()
            .Include(sp => sp.Store)
            .Select(sp => new StoreProductDto
            {
                Id = sp.Id,
                StoreId = sp.StoreId,
                StoreName = sp.Store.Name,
                Name = sp.Name,
                Description = sp.Description,
                PurchasePrice = sp.PurchasePrice,
                CurrentStock = sp.CurrentStock,
                MinimumStock = sp.MinimumStock,
                Unit = sp.Unit,
                IsLowStock = sp.CurrentStock < sp.MinimumStock,
                LastRestocked = sp.LastRestocked,
                CreatedAt = sp.CreatedAt
            })
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (product == null)
            throw new KeyNotFoundException($"Store product with ID {id} not found");

        return product;
    }

    public async Task<StoreProductDto> CreateStoreProductAsync(CreateStoreProductDto dto)
    {
        var storeExists = await context.Stores.AnyAsync(s => s.Id == dto.StoreId);
        if (!storeExists)
            throw new KeyNotFoundException($"Store with ID {dto.StoreId} not found");

        var product = new StoreProduct
        {
            Id = Guid.NewGuid(),
            StoreId = dto.StoreId,
            Name = dto.Name,
            Description = dto.Description,
            PurchasePrice = dto.PurchasePrice,
            CurrentStock = dto.CurrentStock,
            MinimumStock = dto.MinimumStock,
            Unit = dto.Unit,
            LastRestocked = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        context.StoreProducts.Add(product);
        await context.SaveChangesAsync();

        logger.LogInformation("Store product {ProductId} created: {ProductName}", product.Id, product.Name);

        return await GetStoreProductByIdAsync(product.Id);
    }

    public async Task UpdateStoreProductAsync(Guid id, UpdateStoreProductDto dto)
    {
        var product = await context.StoreProducts.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Store product with ID {id} not found");

        if (dto.Name != null) product.Name = dto.Name;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.PurchasePrice.HasValue) product.PurchasePrice = dto.PurchasePrice.Value;
        if (dto.CurrentStock.HasValue) product.CurrentStock = dto.CurrentStock.Value;
        if (dto.MinimumStock.HasValue) product.MinimumStock = dto.MinimumStock.Value;
        if (dto.Unit != null) product.Unit = dto.Unit;

        await context.SaveChangesAsync();
        
        logger.LogInformation("Store product {ProductId} updated", id);
    }

    public async Task DeleteStoreProductAsync(Guid id)
    {
        var product = await context.StoreProducts.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Store product with ID {id} not found");

        context.StoreProducts.Remove(product);
        await context.SaveChangesAsync();

        logger.LogInformation("Store product {ProductId} deleted", id);
    }

    public async Task AdjustInventoryAsync(Guid storeProductId, AdjustInventoryDto dto)
    {
        var product = await context.StoreProducts.FindAsync(storeProductId);
        if (product == null)
            throw new KeyNotFoundException($"Store product with ID {storeProductId} not found");

        var previousStock = product.CurrentStock;
        product.CurrentStock += dto.QuantityChange;

        if (product.CurrentStock < 0)
            throw new InvalidOperationException($"Insufficient stock. Current: {previousStock}, Requested change: {dto.QuantityChange}");

        if (dto.Type == "Restock")
            product.LastRestocked = DateTime.UtcNow;

        context.InventoryLogs.Add(new InventoryLog
        {
            Id = Guid.NewGuid(),
            StoreProductId = storeProductId,
            QuantityChange = dto.QuantityChange,
            Type = Enum.Parse<InventoryLogType>(dto.Type),
            Reason = dto.Reason,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        logger.LogInformation("Inventory adjusted for product {ProductId}: {Change} (Type: {Type})", 
            storeProductId, dto.QuantityChange, dto.Type);
    }

    public async Task<IEnumerable<StoreProductDto>> GetLowStockProductsAsync()
    {
        var products = await context.StoreProducts
            .AsNoTracking()
            .Include(sp => sp.Store)
            .Where(sp => sp.CurrentStock < sp.MinimumStock)
            .Select(sp => new StoreProductDto
            {
                Id = sp.Id,
                StoreId = sp.StoreId,
                StoreName = sp.Store.Name,
                Name = sp.Name,
                Description = sp.Description,
                PurchasePrice = sp.PurchasePrice,
                CurrentStock = sp.CurrentStock,
                MinimumStock = sp.MinimumStock,
                Unit = sp.Unit,
                IsLowStock = true,
                LastRestocked = sp.LastRestocked,
                CreatedAt = sp.CreatedAt
            })
            .ToListAsync();

        return products;
    }

    public async Task<IEnumerable<InventoryLogDto>> GetInventoryLogsAsync(Guid? storeProductId = null, int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        var query = context.InventoryLogs
            .AsNoTracking()
            .Include(il => il.StoreProduct)
            .Where(il => il.CreatedAt >= fromDate)
            .AsQueryable();

        if (storeProductId.HasValue)
            query = query.Where(il => il.StoreProductId == storeProductId);

        var logs = await query
            .OrderByDescending(il => il.CreatedAt)
            .Select(il => new InventoryLogDto
            {
                Id = il.Id,
                StoreProductId = il.StoreProductId,
                StoreProductName = il.StoreProduct.Name,
                QuantityChange = il.QuantityChange,
                Type = il.Type.ToString(),
                Reason = il.Reason,
                CreatedAt = il.CreatedAt
            })
            .ToListAsync();

        return logs;
    }

    public async Task<decimal> GetTotalStockValueAsync(Guid? storeId = null)
    {
        var query = context.StoreProducts.AsNoTracking().AsQueryable();

        if (storeId.HasValue)
            query = query.Where(sp => sp.StoreId == storeId);

        var totalValue = await query
            .SumAsync(sp => sp.CurrentStock * sp.PurchasePrice);

        logger.LogInformation("Total stock value calculated: {Value} (StoreId: {StoreId})", 
            totalValue, storeId?.ToString() ?? "All");

        return totalValue;
    }

    public async Task<List<ConsumptionForecastDto>> GetConsumptionForecastAsync(int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        var products = await context.StoreProducts
            .AsNoTracking()
            .Select(sp => new
            {
                Product = sp,
                TotalConsumed = context.InventoryLogs
                    .Where(il => il.StoreProductId == sp.Id 
                        && il.CreatedAt >= fromDate
                        && (il.Type == InventoryLogType.Sale || il.Type == InventoryLogType.Damage))
                    .Sum(il => Math.Abs(il.QuantityChange))
            })
            .ToListAsync();

        var forecasts = products.Select(p =>
        {
            var avgDailyConsumption = days > 0 ? (double)p.TotalConsumed / days : 0;
            var daysUntilDepletion = avgDailyConsumption > 0 
                ? (int)(p.Product.CurrentStock / avgDailyConsumption) 
                : int.MaxValue;

            return new ConsumptionForecastDto
            {
                StoreProductId = p.Product.Id,
                StoreProductName = p.Product.Name,
                CurrentStock = p.Product.CurrentStock,
                AverageDailyConsumption = Math.Round(avgDailyConsumption, 2),
                EstimatedDaysUntilDepletion = daysUntilDepletion,
                NeedsReorder = p.Product.CurrentStock < p.Product.MinimumStock || daysUntilDepletion < 7,
                Unit = p.Product.Unit
            };
        }).ToList();

        logger.LogInformation("Consumption forecast calculated for {Count} products over {Days} days", 
            forecasts.Count, days);

        return forecasts;
    }

    public async Task DeductIngredientsForOrderItemAsync(Guid productId, int quantity)
    {
        var product = await context.Products
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            logger.LogWarning("Product {ProductId} not found for inventory deduction", productId);
            return;
        }

        foreach (var ingredient in product.ProductIngredients)
        {
            // Calculate required quantity (decimal from recipe * order quantity)
            var requiredQuantity = ingredient.Quantity * quantity;
            var storeProduct = ingredient.StoreProduct;
            
            // Round up to nearest integer (can't deduct partial units)
            var deductAmount = (int)Math.Ceiling(requiredQuantity);
            
            if (storeProduct.CurrentStock >= deductAmount)
            {
                // ✅ FIX: Deduct from stock SAMO - uklonjen LastRestocked
                storeProduct.CurrentStock -= deductAmount;

                // Create inventory log (negative value = deduction)
                var log = new InventoryLog
                {
                    Id = Guid.NewGuid(),
                    StoreProductId = storeProduct.Id,
                    QuantityChange = -deductAmount, // Negative for deduction
                    Type = InventoryLogType.Sale,
                    Reason = $"Order - Product: {product.Name} x{quantity}",
                    CreatedAt = DateTime.UtcNow
                };

                context.InventoryLogs.Add(log);

                logger.LogInformation(
                    "📦 Deducted {Amount} {Unit} of {Ingredient} for {Product} x{Qty}",
                    deductAmount,
                    storeProduct.Unit,
                    storeProduct.Name,
                    product.Name,
                    quantity);

                // Low stock warning
                if (storeProduct.CurrentStock < storeProduct.MinimumStock)
                {
                    logger.LogWarning(
                        "⚠️ LOW STOCK: {Product} - Current: {Current} {Unit}, Minimum: {Min} {Unit}",
                        storeProduct.Name,
                        storeProduct.CurrentStock,
                        storeProduct.Unit,
                        storeProduct.MinimumStock,
                        storeProduct.Unit);
                }
            }
            else
            {
                logger.LogWarning(
                    "❌ INSUFFICIENT STOCK: {Product} - Required: {Required} {Unit}, Available: {Available} {Unit}",
                    storeProduct.Name,
                    deductAmount,
                    storeProduct.Unit,
                    storeProduct.CurrentStock,
                    storeProduct.Unit);
                
                // Still deduct what's available (business decision - adjust if needed)
                // Or throw exception if you want to prevent order creation
            }
        }

        await context.SaveChangesAsync();
    }
}