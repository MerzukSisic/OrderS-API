using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IStoreService
{
    Task<IEnumerable<StoreDto>> GetAllStoresAsync();
    Task<StoreDto> GetStoreByIdAsync(Guid id);
    Task<StoreDto> CreateStoreAsync(CreateStoreDto dto);
    Task UpdateStoreAsync(Guid id, UpdateStoreDto dto);
    Task DeleteStoreAsync(Guid id);
}