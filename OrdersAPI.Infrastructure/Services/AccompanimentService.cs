using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class AccompanimentService(
    ApplicationDbContext context,
    IMapper mapper,
    ILogger<AccompanimentService> logger)
    : IAccompanimentService
{
    public async Task<List<AccompanimentGroupDto>> GetByProductIdAsync(Guid productId)
    {
        logger.LogInformation("Fetching accompaniment groups for product {ProductId}", productId);
        
        var groups = await context.AccompanimentGroups
            .Include(g => g.Accompaniments.OrderBy(a => a.DisplayOrder))
            .Where(g => g.ProductId == productId)
            .OrderBy(g => g.DisplayOrder)
            .ToListAsync();

        return mapper.Map<List<AccompanimentGroupDto>>(groups);
    }

    public async Task<AccompanimentGroupDto?> GetGroupByIdAsync(Guid id)
    {
        var group = await context.AccompanimentGroups
            .Include(g => g.Accompaniments.OrderBy(a => a.DisplayOrder))
            .FirstOrDefaultAsync(g => g.Id == id);

        return group == null ? null : mapper.Map<AccompanimentGroupDto>(group);
    }

    public async Task<AccompanimentGroupDto> CreateGroupAsync(CreateAccompanimentGroupDto dto)
    {
        var productExists = await context.Products.AnyAsync(p => p.Id == dto.ProductId);
        if (!productExists)
        {
            logger.LogWarning("Attempted to create accompaniment group for non-existent product {ProductId}", dto.ProductId);
            throw new KeyNotFoundException($"Product with ID {dto.ProductId} not found");
        }

        var group = new AccompanimentGroup
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ProductId = dto.ProductId,
            SelectionType = Enum.Parse<SelectionType>(dto.SelectionType),
            IsRequired = dto.IsRequired,
            MinSelections = dto.MinSelections,
            MaxSelections = dto.MaxSelections,
            DisplayOrder = dto.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.Accompaniments?.Any() == true)
        {
            foreach (var accDto in dto.Accompaniments)
            {
                group.Accompaniments.Add(new Accompaniment
                {
                    Id = Guid.NewGuid(),
                    Name = accDto.Name,
                    ExtraCharge = accDto.ExtraCharge,
                    DisplayOrder = accDto.DisplayOrder,
                    IsAvailable = accDto.IsAvailable,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        context.AccompanimentGroups.Add(group);
        await context.SaveChangesAsync();

        logger.LogInformation("Created accompaniment group {GroupId} with {Count} accompaniments", 
            group.Id, group.Accompaniments.Count);

        return await GetGroupByIdAsync(group.Id) 
            ?? throw new InvalidOperationException("Failed to retrieve created group");
    }

    public async Task UpdateGroupAsync(Guid id, UpdateAccompanimentGroupDto dto)
    {
        var group = await context.AccompanimentGroups.FindAsync(id);
        if (group == null)
        {
            logger.LogWarning("Attempted to update non-existent accompaniment group {GroupId}", id);
            throw new KeyNotFoundException($"AccompanimentGroup with ID {id} not found");
        }

        group.Name = dto.Name;
        group.SelectionType = Enum.Parse<SelectionType>(dto.SelectionType);
        group.IsRequired = dto.IsRequired;
        group.MinSelections = dto.MinSelections;
        group.MaxSelections = dto.MaxSelections;
        group.DisplayOrder = dto.DisplayOrder;

        await context.SaveChangesAsync();
        
        logger.LogInformation("Updated accompaniment group {GroupId}", id);
    }

    public async Task DeleteGroupAsync(Guid id)
    {
        var group = await context.AccompanimentGroups.FindAsync(id);
        if (group == null)
            throw new KeyNotFoundException($"AccompanimentGroup with ID {id} not found");

        context.AccompanimentGroups.Remove(group);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Deleted accompaniment group {GroupId}", id);
    }

    public async Task<AccompanimentDto?> GetAccompanimentByIdAsync(Guid id)
    {
        var accompaniment = await context.Accompaniments.FindAsync(id);
        return accompaniment == null ? null : mapper.Map<AccompanimentDto>(accompaniment);
    }

    public async Task<AccompanimentDto> AddAccompanimentAsync(Guid groupId, CreateAccompanimentDto dto)
    {
        var groupExists = await context.AccompanimentGroups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new KeyNotFoundException($"AccompanimentGroup with ID {groupId} not found");

        var accompaniment = new Accompaniment
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            ExtraCharge = dto.ExtraCharge,
            AccompanimentGroupId = groupId,
            DisplayOrder = dto.DisplayOrder,
            IsAvailable = dto.IsAvailable,
            CreatedAt = DateTime.UtcNow
        };

        context.Accompaniments.Add(accompaniment);
        await context.SaveChangesAsync();

        logger.LogInformation("Added accompaniment {AccompanimentId} to group {GroupId}", 
            accompaniment.Id, groupId);

        return mapper.Map<AccompanimentDto>(accompaniment);
    }

    public async Task UpdateAccompanimentAsync(Guid id, UpdateAccompanimentDto dto)
    {
        var accompaniment = await context.Accompaniments.FindAsync(id);
        if (accompaniment == null)
            throw new KeyNotFoundException($"Accompaniment with ID {id} not found");

        accompaniment.Name = dto.Name;
        accompaniment.ExtraCharge = dto.ExtraCharge;
        accompaniment.DisplayOrder = dto.DisplayOrder;
        accompaniment.IsAvailable = dto.IsAvailable;

        await context.SaveChangesAsync();
        
        logger.LogInformation("Updated accompaniment {AccompanimentId}", id);
    }

    public async Task DeleteAccompanimentAsync(Guid id)
    {
        var accompaniment = await context.Accompaniments.FindAsync(id);
        if (accompaniment == null)
            throw new KeyNotFoundException($"Accompaniment with ID {id} not found");

        context.Accompaniments.Remove(accompaniment);
        await context.SaveChangesAsync();
        
        logger.LogInformation("Deleted accompaniment {AccompanimentId}", id);
    }

    public async Task<bool> ToggleAvailabilityAsync(Guid id)
    {
        var accompaniment = await context.Accompaniments.FindAsync(id);
        if (accompaniment == null)
            throw new KeyNotFoundException($"Accompaniment with ID {id} not found");

        accompaniment.IsAvailable = !accompaniment.IsAvailable;
        await context.SaveChangesAsync();

        logger.LogInformation("Toggled availability for accompaniment {AccompanimentId} to {IsAvailable}", 
            id, accompaniment.IsAvailable);

        return accompaniment.IsAvailable;
    }

    public async Task<List<AccompanimentDto>> GetAccompanimentsByIdsAsync(List<Guid> accompanimentIds)
    {
        var accompaniments = await context.Accompaniments
            .Where(a => accompanimentIds.Contains(a.Id))
            .ToListAsync();

        return mapper.Map<List<AccompanimentDto>>(accompaniments);
    }

    public async Task<ValidationResult> ValidateSelectionAsync(Guid productId, List<Guid> selectedAccompanimentIds)
    {
        var result = new ValidationResult { IsValid = true };

        var groups = await context.AccompanimentGroups
            .Include(g => g.Accompaniments)
            .Where(g => g.ProductId == productId)
            .ToListAsync();

        if (!groups.Any())
            return result;

        foreach (var group in groups)
        {
            var groupAccompanimentIds = group.Accompaniments.Select(a => a.Id).ToList();
            var selectedFromGroup = selectedAccompanimentIds
                .Where(id => groupAccompanimentIds.Contains(id))
                .ToList();

            if (group.IsRequired && !selectedFromGroup.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"Morate izabrati '{group.Name}'");
                continue;
            }

            if (group.MinSelections.HasValue && selectedFromGroup.Count < group.MinSelections.Value)
            {
                result.IsValid = false;
                result.Errors.Add($"'{group.Name}' zahtijeva minimum {group.MinSelections.Value} izbora");
            }

            if (group.MaxSelections.HasValue && selectedFromGroup.Count > group.MaxSelections.Value)
            {
                result.IsValid = false;
                result.Errors.Add($"'{group.Name}' dozvoljava maksimum {group.MaxSelections.Value} izbora");
            }

            if (group.SelectionType == SelectionType.Single && selectedFromGroup.Count > 1)
            {
                result.IsValid = false;
                result.Errors.Add($"'{group.Name}' dozvoljava samo jedan izbor");
            }

            var unavailableAccompaniments = group.Accompaniments
                .Where(a => selectedFromGroup.Contains(a.Id) && !a.IsAvailable)
                .Select(a => a.Name)
                .ToList();

            if (unavailableAccompaniments.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"Sljedeći prilozi nisu dostupni: {string.Join(", ", unavailableAccompaniments)}");
            }
        }

        if (!result.IsValid)
        {
            logger.LogWarning("Accompaniment validation failed for product {ProductId}: {Errors}", 
                productId, string.Join("; ", result.Errors));
        }

        return result;
    }

    public async Task<decimal> CalculateTotalExtraChargesAsync(List<Guid> accompanimentIds)
    {
        if (!accompanimentIds.Any())
            return 0;

        var totalCharge = await context.Accompaniments
            .Where(a => accompanimentIds.Contains(a.Id))
            .SumAsync(a => a.ExtraCharge);

        return totalCharge;
    }
}
