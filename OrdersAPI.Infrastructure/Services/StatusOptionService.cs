using Microsoft.EntityFrameworkCore;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Exceptions;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class StatusOptionService(ApplicationDbContext context) : IStatusOptionService
{
    public async Task<IEnumerable<StatusOptionDto>> GetAllAsync(string? category = null)
    {
        var query = context.StatusOptions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(category))
            query = query.Where(s => s.Category == category);

        return await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.SortOrder)
            .Select(s => new StatusOptionDto
            {
                Id = s.Id,
                Category = s.Category,
                Name = s.Name,
                DisplayName = s.DisplayName,
                Description = s.Description,
                SortOrder = s.SortOrder,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public async Task<StatusOptionDto> GetByIdAsync(int id)
    {
        var option = await context.StatusOptions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (option == null)
            throw new NotFoundException($"Status option with ID {id} not found");

        return new StatusOptionDto
        {
            Id = option.Id,
            Category = option.Category,
            Name = option.Name,
            DisplayName = option.DisplayName,
            Description = option.Description,
            SortOrder = option.SortOrder,
            IsActive = option.IsActive
        };
    }

    public async Task<StatusOptionDto> CreateAsync(CreateStatusOptionDto dto)
    {
        var exists = await context.StatusOptions
            .AnyAsync(s => s.Category == dto.Category && s.Name == dto.Name);

        if (exists)
            throw new BusinessException($"Status '{dto.Name}' already exists in category '{dto.Category}'");

        var option = new StatusOption
        {
            Category = dto.Category,
            Name = dto.Name,
            DisplayName = dto.DisplayName,
            Description = dto.Description,
            SortOrder = dto.SortOrder,
            IsActive = true
        };

        context.StatusOptions.Add(option);
        await context.SaveChangesAsync();

        return await GetByIdAsync(option.Id);
    }

    public async Task UpdateAsync(int id, UpdateStatusOptionDto dto)
    {
        var option = await context.StatusOptions.FindAsync(id);
        if (option == null)
            throw new NotFoundException($"Status option with ID {id} not found");

        if (dto.DisplayName != null) option.DisplayName = dto.DisplayName;
        if (dto.Description != null) option.Description = dto.Description;
        if (dto.SortOrder.HasValue) option.SortOrder = dto.SortOrder.Value;
        if (dto.IsActive.HasValue) option.IsActive = dto.IsActive.Value;

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var option = await context.StatusOptions.FindAsync(id);
        if (option == null)
            throw new NotFoundException($"Status option with ID {id} not found");

        context.StatusOptions.Remove(option);
        await context.SaveChangesAsync();
    }
}
