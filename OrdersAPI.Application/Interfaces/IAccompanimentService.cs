using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

/// <summary>
/// Service interface for managing product accompaniments (side dishes, vegetables, etc.)
/// </summary>
public interface IAccompanimentService
{
    // ============= AccompanimentGroup Operations =============
    
    /// <summary>
    /// Get all accompaniment groups for a specific product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <returns>List of accompaniment groups with their accompaniments</returns>
    Task<List<AccompanimentGroupDto>> GetByProductIdAsync(Guid productId);

    /// <summary>
    /// Get a specific accompaniment group by ID
    /// </summary>
    /// <param name="id">Accompaniment group ID</param>
    /// <returns>Accompaniment group details or null if not found</returns>
    Task<AccompanimentGroupDto?> GetGroupByIdAsync(Guid id);

    /// <summary>
    /// Create a new accompaniment group for a product
    /// </summary>
    /// <param name="dto">Accompaniment group creation data</param>
    /// <returns>Created accompaniment group</returns>
    Task<AccompanimentGroupDto> CreateGroupAsync(CreateAccompanimentGroupDto dto);

    /// <summary>
    /// Update an existing accompaniment group
    /// </summary>
    /// <param name="id">Accompaniment group ID</param>
    /// <param name="dto">Updated data</param>
    Task UpdateGroupAsync(Guid id, UpdateAccompanimentGroupDto dto);

    /// <summary>
    /// Delete an accompaniment group (cascades to all accompaniments)
    /// </summary>
    /// <param name="id">Accompaniment group ID</param>
    Task DeleteGroupAsync(Guid id);

    // ============= Accompaniment Operations =============

    /// <summary>
    /// Get a specific accompaniment by ID
    /// </summary>
    /// <param name="id">Accompaniment ID</param>
    /// <returns>Accompaniment details or null if not found</returns>
    Task<AccompanimentDto?> GetAccompanimentByIdAsync(Guid id);

    /// <summary>
    /// Add a new accompaniment to an existing group
    /// </summary>
    /// <param name="groupId">Accompaniment group ID</param>
    /// <param name="dto">Accompaniment creation data</param>
    /// <returns>Created accompaniment</returns>
    Task<AccompanimentDto> AddAccompanimentAsync(Guid groupId, CreateAccompanimentDto dto);

    /// <summary>
    /// Update an existing accompaniment
    /// </summary>
    /// <param name="id">Accompaniment ID</param>
    /// <param name="dto">Updated data</param>
    Task UpdateAccompanimentAsync(Guid id, UpdateAccompanimentDto dto);

    /// <summary>
    /// Delete an accompaniment
    /// </summary>
    /// <param name="id">Accompaniment ID</param>
    Task DeleteAccompanimentAsync(Guid id);

    /// <summary>
    /// Toggle accompaniment availability (IsAvailable)
    /// Useful for temporarily disabling items without deleting them
    /// </summary>
    /// <param name="id">Accompaniment ID</param>
    /// <returns>New availability status</returns>
    Task<bool> ToggleAvailabilityAsync(Guid id);

    // ============= Bulk Operations =============

    /// <summary>
    /// Get all accompaniments by their IDs (used during order creation)
    /// </summary>
    /// <param name="accompanimentIds">List of accompaniment IDs</param>
    /// <returns>List of accompaniments</returns>
    Task<List<AccompanimentDto>> GetAccompanimentsByIdsAsync(List<Guid> accompanimentIds);

    /// <summary>
    /// Validate selected accompaniments against group rules
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="selectedAccompanimentIds">List of selected accompaniment IDs</param>
    /// <returns>Validation result with error messages if invalid</returns>
    Task<ValidationResult> ValidateSelectionAsync(Guid productId, List<Guid> selectedAccompanimentIds);

    /// <summary>
    /// Calculate total extra charges for selected accompaniments
    /// </summary>
    /// <param name="accompanimentIds">List of accompaniment IDs</param>
    /// <returns>Total extra charge amount</returns>
    Task<decimal> CalculateTotalExtraChargesAsync(List<Guid> accompanimentIds);
}


public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}