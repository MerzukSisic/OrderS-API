using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IStatusOptionService
{
    Task<IEnumerable<StatusOptionDto>> GetAllAsync(string? category = null);
    Task<StatusOptionDto> GetByIdAsync(int id);
    Task<StatusOptionDto> CreateAsync(CreateStatusOptionDto dto);
    Task UpdateAsync(int id, UpdateStatusOptionDto dto);
    Task DeleteAsync(int id);
}
