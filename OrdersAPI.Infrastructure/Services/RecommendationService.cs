using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(ApplicationDbContext context, IMapper mapper, ILogger<RecommendationService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetRecommendedProductsAsync(Guid? userId = null, int count = 5)
    {
        var recommendations = new List<Product>();

        // 1. TIME-BASED RECOMMENDATIONS (prema dobu dana)
        var hour = DateTime.Now.Hour;
        var timeBasedProducts = await GetTimeBasedProductsAsync(hour);
        recommendations.AddRange(timeBasedProducts.Take(2));

        // 2. POPULAR PRODUCTS (najprodavaniji)
        var popularProducts = await GetPopularProductsInternalAsync(3);
        recommendations.AddRange(popularProducts.Where(p => !recommendations.Any(r => r.Id == p.Id)));

        // 3. USER-BASED (ako postoji userId - collaborative filtering)
        if (userId.HasValue)
        {
            var userBasedProducts = await GetUserBasedRecommendationsAsync(userId.Value);
            recommendations.AddRange(userBasedProducts.Where(p => !recommendations.Any(r => r.Id == p.Id)).Take(2));
        }

        // Vrati unique proizvode
        var uniqueRecommendations = recommendations
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .Take(count)
            .ToList();

        return _mapper.Map<IEnumerable<ProductDto>>(uniqueRecommendations);
    }

    public async Task<IEnumerable<ProductDto>> GetPopularProductsAsync(int count = 10)
    {
        var products = await GetPopularProductsInternalAsync(count);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<IEnumerable<ProductDto>> GetTimeBasedRecommendationsAsync(int hour, int count = 5)
    {
        var products = await GetTimeBasedProductsAsync(hour, count);
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    // PRIVATE HELPER METHODS

    private async Task<List<Product>> GetPopularProductsInternalAsync(int count)
    {
        var last30Days = DateTime.UtcNow.AddDays(-30);

        var popularProductIds = await _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.CreatedAt >= last30Days && oi.Order.Status == OrderStatus.Completed)
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

        return await _context.Products
            .Include(p => p.Category)
            .Where(p => popularProductIds.Contains(p.Id) && p.IsAvailable)
            .ToListAsync();
    }

    private async Task<List<Product>> GetTimeBasedProductsAsync(int hour, int count = 5)
    {
        // BREAKFAST (6-11h): Kafa, sendviči
        if (hour >= 6 && hour < 11)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && 
                           (p.Name.Contains("Kafa") || 
                            p.Name.Contains("Coffee") || 
                            p.Name.Contains("Sendvič") ||
                            p.Name.Contains("Doručak")))
                .Take(count)
                .ToListAsync();
        }
        // LUNCH (11-15h): Jela, glavna jela
        else if (hour >= 11 && hour < 15)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && p.Category.Name.Contains("Hrana"))
                .Take(count)
                .ToListAsync();
        }
        // AFTERNOON/DESSERT (15-18h): Deserti, kafa
        else if (hour >= 15 && hour < 18)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && 
                           (p.Category.Name.Contains("Desert") || 
                            p.Name.Contains("Kafa")))
                .Take(count)
                .ToListAsync();
        }
        // EVENING (18-23h): Piće, lagana hrana
        else
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && p.Category.Name.Contains("Piće"))
                .Take(count)
                .ToListAsync();
        }
    }

    private async Task<List<Product>> GetUserBasedRecommendationsAsync(Guid userId)
    {
        // Collaborative Filtering: Pronađi šta naručuju slični korisnici

        // 1. Uzmi proizvode koje je korisnik naručio
        var userProductIds = await _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.WaiterId == userId)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync();

        if (!userProductIds.Any())
            return new List<Product>();

        // 2. Pronađi druge korisnike koji su naručivali iste proizvode
        var similarUserIds = await _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => userProductIds.Contains(oi.ProductId) && oi.Order.WaiterId != userId)
            .Select(oi => oi.Order.WaiterId)
            .Distinct()
            .Take(20)
            .ToListAsync();

        if (!similarUserIds.Any())
            return new List<Product>();

        // 3. Vrati proizvode koje su naručili slični korisnici, a korisnik nije
        var recommendedProductIds = await _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => similarUserIds.Contains(oi.Order.WaiterId) && 
                        !userProductIds.Contains(oi.ProductId))
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .Select(x => x.ProductId)
            .ToListAsync();

        return await _context.Products
            .Include(p => p.Category)
            .Where(p => recommendedProductIds.Contains(p.Id) && p.IsAvailable)
            .ToListAsync();
    }
}

