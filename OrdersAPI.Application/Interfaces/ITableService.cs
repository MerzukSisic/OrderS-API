using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;

namespace OrdersAPI.Application.Interfaces;

public interface ITableService
{
    Task<IEnumerable<TableDto>> GetAllTablesAsync();
    Task<TableDto> GetTableByIdAsync(Guid id);
    Task<TableDto> CreateTableAsync(CreateTableDto dto);
    Task UpdateTableAsync(Guid id, UpdateTableDto dto);
    Task UpdateTableStatusAsync(Guid id, TableStatus status);
    Task DeleteTableAsync(Guid id);
}
