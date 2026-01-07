using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class AccompanimentService : IAccompanimentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public AccompanimentService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // ============= AccompanimentGroup Operations =============

    public async Task<List<AccompanimentGroupDto>> GetByProductIdAsync(Guid productId)
    {
        var groups = await _context.AccompanimentGroups
            .Include(g => g.Accompaniments.OrderBy(a => a.DisplayOrder))
            .Where(g => g.ProductId == productId)
            .OrderBy(g => g.DisplayOrder)
            .ToListAsync();

        return _mapper.Map<List<AccompanimentGroupDto>>(groups);
    }

    public async Task<AccompanimentGroupDto?> GetGroupByIdAsync(Guid id)
    {
        var group = await _context.AccompanimentGroups
            .Include(g => g.Accompaniments.OrderBy(a => a.DisplayOrder))
            .FirstOrDefaultAsync(g => g.Id == id);

        return group == null ? null : _mapper.Map<AccompanimentGroupDto>(group);
    }

    public async Task<AccompanimentGroupDto> CreateGroupAsync(CreateAccompanimentGroupDto dto)
    {
        // Validate product exists
        var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId);
        if (!productExists)
            throw new InvalidOperationException($"Product with ID {dto.ProductId} not found");

        // Create group
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

        // Add accompaniments if provided
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

        _context.AccompanimentGroups.Add(group);
        await _context.SaveChangesAsync();

        return await GetGroupByIdAsync(group.Id) 
            ?? throw new InvalidOperationException("Failed to retrieve created group");
    }

    public async Task UpdateGroupAsync(Guid id, UpdateAccompanimentGroupDto dto)
    {
        var group = await _context.AccompanimentGroups.FindAsync(id);
        if (group == null)
            throw new InvalidOperationException($"AccompanimentGroup with ID {id} not found");

        group.Name = dto.Name;
        group.SelectionType = Enum.Parse<SelectionType>(dto.SelectionType);
        group.IsRequired = dto.IsRequired;
        group.MinSelections = dto.MinSelections;
        group.MaxSelections = dto.MaxSelections;
        group.DisplayOrder = dto.DisplayOrder;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteGroupAsync(Guid id)
    {
        var group = await _context.AccompanimentGroups.FindAsync(id);
        if (group == null)
            throw new InvalidOperationException($"AccompanimentGroup with ID {id} not found");

        _context.AccompanimentGroups.Remove(group);
        await _context.SaveChangesAsync();
    }

    // ============= Accompaniment Operations =============

    public async Task<AccompanimentDto?> GetAccompanimentByIdAsync(Guid id)
    {
        var accompaniment = await _context.Accompaniments.FindAsync(id);
        return accompaniment == null ? null : _mapper.Map<AccompanimentDto>(accompaniment);
    }

    public async Task<AccompanimentDto> AddAccompanimentAsync(Guid groupId, CreateAccompanimentDto dto)
    {
        // Validate group exists
        var groupExists = await _context.AccompanimentGroups.AnyAsync(g => g.Id == groupId);
        if (!groupExists)
            throw new InvalidOperationException($"AccompanimentGroup with ID {groupId} not found");

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

        _context.Accompaniments.Add(accompaniment);
        await _context.SaveChangesAsync();

        return _mapper.Map<AccompanimentDto>(accompaniment);
    }

    public async Task UpdateAccompanimentAsync(Guid id, UpdateAccompanimentDto dto)
    {
        var accompaniment = await _context.Accompaniments.FindAsync(id);
        if (accompaniment == null)
            throw new InvalidOperationException($"Accompaniment with ID {id} not found");

        accompaniment.Name = dto.Name;
        accompaniment.ExtraCharge = dto.ExtraCharge;
        accompaniment.DisplayOrder = dto.DisplayOrder;
        accompaniment.IsAvailable = dto.IsAvailable;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAccompanimentAsync(Guid id)
    {
        var accompaniment = await _context.Accompaniments.FindAsync(id);
        if (accompaniment == null)
            throw new InvalidOperationException($"Accompaniment with ID {id} not found");

        _context.Accompaniments.Remove(accompaniment);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ToggleAvailabilityAsync(Guid id)
    {
        var accompaniment = await _context.Accompaniments.FindAsync(id);
        if (accompaniment == null)
            throw new InvalidOperationException($"Accompaniment with ID {id} not found");

        accompaniment.IsAvailable = !accompaniment.IsAvailable;
        await _context.SaveChangesAsync();

        return accompaniment.IsAvailable;
    }

    // ============= Bulk Operations =============

    public async Task<List<AccompanimentDto>> GetAccompanimentsByIdsAsync(List<Guid> accompanimentIds)
    {
        var accompaniments = await _context.Accompaniments
            .Where(a => accompanimentIds.Contains(a.Id))
            .ToListAsync();

        return _mapper.Map<List<AccompanimentDto>>(accompaniments);
    }

    public async Task<ValidationResult> ValidateSelectionAsync(Guid productId, List<Guid> selectedAccompanimentIds)
    {
        var result = new ValidationResult { IsValid = true };

        // Get all groups for this product
        var groups = await _context.AccompanimentGroups
            .Include(g => g.Accompaniments)
            .Where(g => g.ProductId == productId)
            .ToListAsync();

        if (!groups.Any())
            return result; // No accompaniments required

        foreach (var group in groups)
        {
            // Get selected accompaniments for this group
            var groupAccompanimentIds = group.Accompaniments.Select(a => a.Id).ToList();
            var selectedFromGroup = selectedAccompanimentIds
                .Where(id => groupAccompanimentIds.Contains(id))
                .ToList();

            // Check if required
            if (group.IsRequired && !selectedFromGroup.Any())
            {
                result.IsValid = false;
                result.Errors.Add($"Morate izabrati '{group.Name}'");
                continue;
            }

            // Check minimum selections
            if (group.MinSelections.HasValue && selectedFromGroup.Count < group.MinSelections.Value)
            {
                result.IsValid = false;
                result.Errors.Add($"'{group.Name}' zahtijeva minimum {group.MinSelections.Value} izbora");
            }

            // Check maximum selections
            if (group.MaxSelections.HasValue && selectedFromGroup.Count > group.MaxSelections.Value)
            {
                result.IsValid = false;
                result.Errors.Add($"'{group.Name}' dozvoljava maksimum {group.MaxSelections.Value} izbora");
            }

            // Check Single selection type
            if (group.SelectionType == SelectionType.Single && selectedFromGroup.Count > 1)
            {
                result.IsValid = false;
                result.Errors.Add($"'{group.Name}' dozvoljava samo jedan izbor");
            }

            // Check availability
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

        return result;
    }

    public async Task<decimal> CalculateTotalExtraChargesAsync(List<Guid> accompanimentIds)
    {
        if (!accompanimentIds.Any())
            return 0;

        var totalCharge = await _context.Accompaniments
            .Where(a => accompanimentIds.Contains(a.Id))
            .SumAsync(a => a.ExtraCharge);

        return totalCharge;
    }
}