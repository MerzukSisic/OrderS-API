using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class RecommendationService(
    ApplicationDbContext context,
    ILogger<RecommendationService> logger) : IRecommendationService
{
    private const int POPULAR_PRODUCTS_DAYS = 30;
    private const int SIMILAR_USERS_LIMIT = 20;

    public async Task<IEnumerable<ProductDto>> GetRecommendedProductsAsync(Guid? userId = null, int count = 5)
    {
        var recommendations = new List<Product>();

        // 1. TIME-BASED (2 items)
        var hour = DateTime.UtcNow.Hour;
        var timeBasedProducts = await GetTimeBasedProductsInternalAsync(hour);
        recommendations.AddRange(timeBasedProducts.Take(2));

        // 2. POPULAR PRODUCTS (3 items)
        var popularProducts = await GetPopularProductsInternalAsync(3);
        recommendations.AddRange(popularProducts.Where(p => !recommendations.Any(r => r.Id == p.Id)));

        // 3. USER-BASED (2 items) - ako postoji userId
        if (userId.HasValue)
        {
            var userBasedProducts = await GetUserBasedRecommendationsInternalAsync(userId.Value);
            recommendations.AddRange(userBasedProducts.Where(p => !recommendations.Any(r => r.Id == p.Id)).Take(2));
        }

        // Fallback - ako nema dovoljno preporuka, dodaj random available products
        if (recommendations.Count < count)
        {
            var fallbackProducts = await context.Products
                .AsNoTracking()
                .Where(p => p.IsAvailable && !recommendations.Select(r => r.Id).Contains(p.Id))
                .OrderBy(p => Guid.NewGuid()) // Random
                .Take(count - recommendations.Count)
                .ToListAsync();

            recommendations.AddRange(fallbackProducts);
        }

        // Map to DTO
        var uniqueRecommendations = recommendations
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .Take(count)
            .ToList();

        var result = await MapToProductDtos(uniqueRecommendations);

        logger.LogInformation("Generated {Count} recommendations for user {UserId}", 
            result.Count(), userId);

        return result;
    }

    public async Task<IEnumerable<ProductDto>> GetPopularProductsAsync(int count = 10)
    {
        var products = await GetPopularProductsInternalAsync(count);
        var result = await MapToProductDtos(products);

        logger.LogInformation("Retrieved {Count} popular products", result.Count());

        return result;
    }

    public async Task<IEnumerable<ProductDto>> GetTimeBasedRecommendationsAsync(int hour, int count = 5)
    {
        var products = await GetTimeBasedProductsInternalAsync(hour, count);
        var result = await MapToProductDtos(products);

        logger.LogInformation("Retrieved {Count} time-based recommendations for hour {Hour}", 
            result.Count(), hour);

        return result;
    }

    // ========== PRIVATE HELPER METHODS ==========

    private async Task<List<Product>> GetPopularProductsInternalAsync(int count)
    {
        var startDate = DateTime.UtcNow.AddDays(-POPULAR_PRODUCTS_DAYS);

        var popularProductIds = await context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.CreatedAt >= startDate && 
                        oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalQuantity = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Subtotal)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .ThenByDescending(x => x.TotalRevenue)
            .Take(count)
            .Select(x => x.ProductId)
            .ToListAsync();

        return await context.Products
            .AsNoTracking()
            .Where(p => popularProductIds.Contains(p.Id) && p.IsAvailable)
            .ToListAsync();
    }

    private async Task<List<Product>> GetTimeBasedProductsInternalAsync(int hour, int count = 5)
    {
        // ✅ Refactored - koristi Category-based filtering
        var categoryFilters = GetCategoryFiltersForHour(hour);

        var query = context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsAvailable)
            .AsQueryable();

        // Apply category filters
        if (categoryFilters.Any())
        {
            query = query.Where(p => categoryFilters.Contains(p.Category.Name));
        }

        return await query
            .OrderByDescending(p => p.Stock) // Prioritize available items
            .Take(count)
            .ToListAsync();
    }

    private async Task<List<Product>> GetUserBasedRecommendationsInternalAsync(Guid userId)
    {
        // Collaborative Filtering: Find similar users and recommend their products

        // 1. Get user's order history
        var userProductIds = await context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.WaiterId == userId) // Assuming WaiterId is the user placing order
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync();

        if (!userProductIds.Any())
        {
            logger.LogInformation("No order history for user {UserId}, returning empty", userId);
            return new List<Product>();
        }

        // 2. Find similar users (users who ordered the same products)
        var similarUserIds = await context.OrderItems
            .AsNoTracking()
            .Where(oi => userProductIds.Contains(oi.ProductId) && oi.Order.WaiterId != userId)
            .Select(oi => oi.Order.WaiterId)
            .Distinct()
            .Take(SIMILAR_USERS_LIMIT)
            .ToListAsync();

        if (!similarUserIds.Any())
            return new List<Product>();

        // 3. Get products ordered by similar users but not by current user
        var recommendedProductIds = await context.OrderItems
            .AsNoTracking()
            .Where(oi => similarUserIds.Contains(oi.Order.WaiterId) && 
                        !userProductIds.Contains(oi.ProductId))
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Score = g.Count() // How many similar users ordered this
            })
            .OrderByDescending(x => x.Score)
            .Take(5)
            .Select(x => x.ProductId)
            .ToListAsync();

        return await context.Products
            .AsNoTracking()
            .Where(p => recommendedProductIds.Contains(p.Id) && p.IsAvailable)
            .ToListAsync();
    }

    private static List<string> GetCategoryFiltersForHour(int hour)
    {
        // BREAKFAST (6-11h)
        if (hour >= 6 && hour < 11)
            return new List<string> { "Doručak", "Kafa", "Topli napici", "Sokovi" };

        // LUNCH (11-15h)
        if (hour >= 11 && hour < 15)
            return new List<string> { "Glavna jela", "Hrana", "Sendviči", "Burgeri" };

        // AFTERNOON/COFFEE (15-18h)
        if (hour >= 15 && hour < 18)
            return new List<string> { "Deserti", "Kafa", "Torte", "Slatkiši" };

        // EVENING/DRINKS (18-23h)
        return new List<string> { "Piće", "Alkoholna pića", "Bezalkoholna pića", "Kokteli" };
    }

    private async Task<List<ProductDto>> MapToProductDtos(List<Product> products)
    {
        var productIds = products.Select(p => p.Id).ToList();

        // Load full product data with relationships
        var fullProducts = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        return fullProducts.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            ImageUrl = p.ImageUrl,
            IsAvailable = p.IsAvailable,
            PreparationLocation = p.Location.ToString(),
            PreparationTimeMinutes = p.PreparationTimeMinutes,
            Stock = p.Stock,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Ingredients = p.ProductIngredients.Select(pi => new ProductIngredientDto
            {
                Id = pi.Id,
                StoreProductId = pi.StoreProductId,
                StoreProductName = pi.StoreProduct.Name,
                Quantity = pi.Quantity,
                Unit = pi.StoreProduct.Unit.ToString()
            }).ToList(),
            AccompanimentGroups = new List<AccompanimentGroupDto>()
        }).ToList();
    }
}
