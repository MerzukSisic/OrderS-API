using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync(Guid? categoryId = null, bool? isAvailable = null);
    Task<ProductDto> GetProductByIdAsync(Guid id);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto);
    Task UpdateProductAsync(Guid id, UpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
    
    Task<bool> ToggleAvailabilityAsync(Guid productId);
    Task<List<ProductDto>> GetProductsByLocationAsync(PreparationLocation location, bool? isAvailable = null);
    Task BulkUpdateAvailabilityAsync(List<Guid> productIds, bool isAvailable);
}
