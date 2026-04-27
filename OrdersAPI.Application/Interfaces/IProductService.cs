using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllProductsAsync(Guid? categoryId = null, bool? isAvailable = null, int page = 1, int pageSize = 50);
    Task<ProductDto> GetProductByIdAsync(Guid id);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto);
    Task UpdateProductAsync(Guid id, UpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
    Task<PagedResult<ProductDto>> SearchProductsAsync(string searchTerm, int page = 1, int pageSize = 50);

    Task<bool> ToggleAvailabilityAsync(Guid productId);
    Task<PagedResult<ProductDto>> GetProductsByLocationAsync(PreparationLocation location, bool? isAvailable = null, int page = 1, int pageSize = 50);
    Task BulkUpdateAvailabilityAsync(List<Guid> productIds, bool isAvailable);
}
