using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class InventoryService(ApplicationDbContext context, IMapper mapper, ILogger<InventoryService> logger)
    : IInventoryService
{
    public async Task<IEnumerable<StoreProductDto>> GetAllStoreProductsAsync(Guid? storeId = null)
    {
        var query = context.StoreProducts
            .Include(sp => sp.Store)
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(sp => sp.StoreId == storeId);

        var products = await query.ToListAsync();
        return mapper.Map<IEnumerable<StoreProductDto>>(products);
    }

    public async Task<StoreProductDto> GetStoreProductByIdAsync(Guid id)
    {
        var product = await context.StoreProducts
            .Include(sp => sp.Store)
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (product == null)
            throw new KeyNotFoundException($"Store product {id} not found");

        return mapper.Map<StoreProductDto>(product);
    }

    public async Task<StoreProductDto> CreateStoreProductAsync(CreateStoreProductDto dto)
    {
        var product = new StoreProduct
        {
            Id = Guid.NewGuid(),
            StoreId = dto.StoreId,
            Name = dto.Name,
            Description = dto.Description,
            PurchasePrice = dto.PurchasePrice,
            CurrentStock = dto.CurrentStock,
            MinimumStock = dto.MinimumStock,
            Unit = dto.Unit
        };

        context.StoreProducts.Add(product);
        await context.SaveChangesAsync();

        logger.LogInformation("Store product {ProductId} created: {ProductName}", product.Id, product.Name);

        return mapper.Map<StoreProductDto>(product);
    }

    public async Task UpdateStoreProductAsync(Guid id, UpdateStoreProductDto dto)
    {
        var product = await context.StoreProducts.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Store product {id} not found");

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
            throw new KeyNotFoundException($"Store product {id} not found");

        context.StoreProducts.Remove(product);
        await context.SaveChangesAsync();

        logger.LogInformation("Store product {ProductId} deleted", id);
    }

    public async Task AdjustInventoryAsync(Guid storeProductId, AdjustInventoryDto dto)
    {
        var product = await context.StoreProducts.FindAsync(storeProductId);
        if (product == null)
            throw new KeyNotFoundException($"Store product {storeProductId} not found");

        product.CurrentStock += dto.QuantityChange;

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

        logger.LogInformation("Inventory adjusted for product {ProductId}: {Change}", storeProductId, dto.QuantityChange);
    }

    public async Task<IEnumerable<StoreProductDto>> GetLowStockProductsAsync()
    {
        var products = await context.StoreProducts
            .Include(sp => sp.Store)
            .Where(sp => sp.CurrentStock < sp.MinimumStock)
            .ToListAsync();

        return mapper.Map<IEnumerable<StoreProductDto>>(products);
    }

    public async Task<IEnumerable<InventoryLogDto>> GetInventoryLogsAsync(Guid? storeProductId = null, int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        var query = context.InventoryLogs
            .Include(il => il.StoreProduct)
            .Where(il => il.CreatedAt >= fromDate)
            .AsQueryable();

        if (storeProductId.HasValue)
            query = query.Where(il => il.StoreProductId == storeProductId);

        var logs = await query
            .OrderByDescending(il => il.CreatedAt)
            .ToListAsync();

        return mapper.Map<IEnumerable<InventoryLogDto>>(logs);
    }
}

