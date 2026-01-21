using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class CategoryService(ApplicationDbContext context, ILogger<CategoryService> logger)
    : ICategoryService
{
    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await context.Categories
            .AsNoTracking()
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IconName = c.IconName,
                ProductCount = c.Products.Count,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return categories;
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(Guid id)
    {
        var category = await context.Categories
            .AsNoTracking()
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IconName = c.IconName,
                ProductCount = c.Products.Count,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            throw new KeyNotFoundException($"Category with ID {id} not found");

        return category;
    }

    public async Task<CategoryWithProductsDto> GetCategoryWithProductsAsync(Guid id)
    {
        var category = await context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            throw new KeyNotFoundException($"Category with ID {id} not found");

        var dto = new CategoryWithProductsDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IconName = category.IconName,
            CreatedAt = category.CreatedAt,
            Products = category.Products.Select(p => new ProductSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = category.Name,
                ImageUrl = p.ImageUrl,
                IsAvailable = p.IsAvailable,
                PreparationLocation = p.Location.ToString(),
                PreparationTimeMinutes = p.PreparationTimeMinutes
            }).ToList()
        };

        return dto;
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            IconName = dto.IconName,
            CreatedAt = DateTime.UtcNow
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        logger.LogInformation("Category {CategoryId} created: {CategoryName}", category.Id, category.Name);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IconName = category.IconName,
            ProductCount = 0,
            CreatedAt = category.CreatedAt
        };
    }

    public async Task UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await context.Categories.FindAsync(id);
        if (category == null)
            throw new KeyNotFoundException($"Category with ID {id} not found");

        if (dto.Name != null) category.Name = dto.Name;
        if (dto.Description != null) category.Description = dto.Description;
        if (dto.IconName != null) category.IconName = dto.IconName;

        await context.SaveChangesAsync();
        
        logger.LogInformation("Category {CategoryId} updated", id);
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var category = await context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            throw new KeyNotFoundException($"Category with ID {id} not found");

        if (category.Products.Any())
            throw new InvalidOperationException($"Cannot delete category with {category.Products.Count} products. Delete or reassign products first.");

        context.Categories.Remove(category);
        await context.SaveChangesAsync();

        logger.LogInformation("Category {CategoryId} deleted", id);
    }
}
