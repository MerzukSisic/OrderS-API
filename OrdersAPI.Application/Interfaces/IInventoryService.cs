using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IInventoryService
{
    Task<IEnumerable<StoreProductDto>> GetAllStoreProductsAsync(Guid? storeId = null);
    Task<StoreProductDto> GetStoreProductByIdAsync(Guid id);
    Task<StoreProductDto> CreateStoreProductAsync(CreateStoreProductDto dto);
    Task UpdateStoreProductAsync(Guid id, UpdateStoreProductDto dto);
    Task DeleteStoreProductAsync(Guid id);
    Task AdjustInventoryAsync(Guid storeProductId, AdjustInventoryDto dto);
    Task<IEnumerable<StoreProductDto>> GetLowStockProductsAsync();
    Task<IEnumerable<InventoryLogDto>> GetInventoryLogsAsync(Guid? storeProductId = null, int days = 30);
}
