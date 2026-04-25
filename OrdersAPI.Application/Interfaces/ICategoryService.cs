using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetAllCategoriesAsync(int page = 1, int pageSize = 100);
    Task<CategoryDto> GetCategoryByIdAsync(Guid id);
    Task<CategoryWithProductsDto> GetCategoryWithProductsAsync(Guid id);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);
    Task DeleteCategoryAsync(Guid id);
}