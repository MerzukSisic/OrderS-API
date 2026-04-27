using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IAccompanimentService
{
    Task<List<AccompanimentGroupDto>> GetByProductIdAsync(Guid productId);
    Task<AccompanimentGroupDto?> GetGroupByIdAsync(Guid id);
    Task<AccompanimentGroupDto> CreateGroupAsync(CreateAccompanimentGroupDto dto);
    Task UpdateGroupAsync(Guid id, UpdateAccompanimentGroupDto dto);
    Task DeleteGroupAsync(Guid id);
    Task<AccompanimentDto?> GetAccompanimentByIdAsync(Guid id);
    Task<AccompanimentDto> AddAccompanimentAsync(Guid groupId, CreateAccompanimentDto dto);
    Task UpdateAccompanimentAsync(Guid id, UpdateAccompanimentDto dto);
    Task DeleteAccompanimentAsync(Guid id);
    Task<bool> ToggleAvailabilityAsync(Guid id);
    Task<List<AccompanimentDto>> GetAccompanimentsByIdsAsync(List<Guid> accompanimentIds);
    Task<ValidationResult> ValidateSelectionAsync(Guid productId, List<Guid> selectedAccompanimentIds);
    Task<decimal> CalculateTotalExtraChargesAsync(List<Guid> accompanimentIds);
}


public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}