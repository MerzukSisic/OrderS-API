using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync(Guid? categoryId = null, bool? isAvailable = null);
    Task<ProductDto> GetProductByIdAsync(Guid id);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto);
    Task UpdateProductAsync(Guid id, UpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
}
