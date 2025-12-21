using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ApplicationDbContext context, IMapper mapper, ILogger<CategoryService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _context.Categories
            .Include(c => c.Products)
            .ToListAsync();

        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
            throw new KeyNotFoundException($"Category {id} not found");

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            IconName = dto.IconName
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Category {CategoryId} created: {CategoryName}", category.Id, category.Name);

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            throw new KeyNotFoundException($"Category {id} not found");

        if (dto.Name != null) category.Name = dto.Name;
        if (dto.Description != null) category.Description = dto.Description;
        if (dto.IconName != null) category.IconName = dto.IconName;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Category {CategoryId} updated", id);
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            throw new KeyNotFoundException($"Category {id} not found");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Category {CategoryId} deleted", id);
    }
}

