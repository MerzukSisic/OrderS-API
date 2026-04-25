using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Application.Interfaces;

public interface ITableService
{
    Task<PagedResult<TableDto>> GetAllTablesAsync(int page = 1, int pageSize = 100);
    Task<TableDto> GetTableByIdAsync(Guid id);
    Task<TableDto> CreateTableAsync(CreateTableDto dto);
    Task UpdateTableAsync(Guid id, UpdateTableDto dto);
    Task UpdateTableStatusAsync(Guid id, TableStatus status);
    Task DeleteTableAsync(Guid id);
}
