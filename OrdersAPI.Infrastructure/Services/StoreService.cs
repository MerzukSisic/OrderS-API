using Microsoft.EntityFrameworkCore;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class StoreService(ApplicationDbContext context) : IStoreService
{
    public async Task<IEnumerable<StoreDto>> GetAllStoresAsync()
    {
        return await context.Stores
            .Include(s => s.StoreProducts)
            .Select(s => new StoreDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Address = s.Address,
                CreatedAt = s.CreatedAt,
                TotalProducts = s.StoreProducts.Count,
                LowStockProductsCount = s.StoreProducts.Count(p => p.CurrentStock < p.MinimumStock)
            })
            .ToListAsync();
    }

    public async Task<StoreDto> GetStoreByIdAsync(Guid id)
    {
        var store = await context.Stores
            .Include(s => s.StoreProducts)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (store == null)
            throw new KeyNotFoundException($"Store with ID {id} not found");

        return new StoreDto
        {
            Id = store.Id,
            Name = store.Name,
            Description = store.Description,
            Address = store.Address,
            CreatedAt = store.CreatedAt,
            TotalProducts = store.StoreProducts.Count,
            LowStockProductsCount = store.StoreProducts.Count(p => p.CurrentStock < p.MinimumStock)
        };
    }

    public async Task<StoreDto> CreateStoreAsync(CreateStoreDto dto)
    {
        var store = new Store
        {
            Name = dto.Name,
            Description = dto.Description,
            Address = dto.Address,
            CreatedAt = DateTime.UtcNow
        };

        context.Stores.Add(store);
        await context.SaveChangesAsync();

        return await GetStoreByIdAsync(store.Id);
    }

    public async Task UpdateStoreAsync(Guid id, UpdateStoreDto dto)
    {
        var store = await context.Stores.FindAsync(id);
        if (store == null)
            throw new KeyNotFoundException($"Store with ID {id} not found");

        if (!string.IsNullOrEmpty(dto.Name))
            store.Name = dto.Name;

        if (dto.Description != null)
            store.Description = dto.Description;

        if (dto.Address != null)
            store.Address = dto.Address;

        await context.SaveChangesAsync();
    }

    public async Task DeleteStoreAsync(Guid id)
    {
        var store = await context.Stores
            .Include(s => s.StoreProducts)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (store == null)
            throw new KeyNotFoundException($"Store with ID {id} not found");

        // Provjeri da li ima proizvoda
        if (store.StoreProducts.Any())
            throw new InvalidOperationException("Cannot delete store with products. Remove products first.");

        context.Stores.Remove(store);
        await context.SaveChangesAsync();
    }
}
