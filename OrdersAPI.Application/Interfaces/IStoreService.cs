using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IStoreService
{
    Task<PagedResult<StoreDto>> GetAllStoresAsync(int page = 1, int pageSize = 100);
    Task<StoreDto> GetStoreByIdAsync(Guid id);
    Task<StoreDto> CreateStoreAsync(CreateStoreDto dto);
    Task UpdateStoreAsync(Guid id, UpdateStoreDto dto);
    Task DeleteStoreAsync(Guid id);
}