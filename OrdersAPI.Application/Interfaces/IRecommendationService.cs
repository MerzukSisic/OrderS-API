using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IRecommendationService
{
    Task<IEnumerable<ProductDto>> GetRecommendedProductsAsync(Guid? userId = null, int count = 5);
    Task<IEnumerable<ProductDto>> GetPopularProductsAsync(int count = 10);
    Task<IEnumerable<ProductDto>> GetTimeBasedRecommendationsAsync(int hour, int count = 5);
}
