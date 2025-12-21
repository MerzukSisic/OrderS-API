using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto> GetCategoryByIdAsync(Guid id);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);
    Task DeleteCategoryAsync(Guid id);
}
