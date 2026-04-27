using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IRecommendationService
{
    Task<IEnumerable<RecommendedProductDto>> GetRecommendedProductsAsync(Guid? userId = null, int count = 5);
    Task<IEnumerable<RecommendedProductDto>> GetPopularProductsAsync(int count = 10);
    Task<IEnumerable<RecommendedProductDto>> GetTimeBasedRecommendationsAsync(int hour, int count = 5);
}
