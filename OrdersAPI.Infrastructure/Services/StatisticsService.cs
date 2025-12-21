using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class StatisticsService(ApplicationDbContext context, IMapper mapper, ILogger<StatisticsService> logger)
    : IStatisticsService
{
    private readonly ILogger<StatisticsService> _logger = logger;

    public async Task<DashboardDto> GetDashboardStatsAsync()
    {
        var today = DateTime.Today;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddMonths(-1);

        // Today stats
        var todayOrders = await context.Orders
            .Where(o => o.CreatedAt >= today && o.Status == OrderStatus.Completed)
            .ToListAsync();

        var todayRevenue = todayOrders.Sum(o => o.TotalAmount);

        // Week stats
        var weekOrders = await context.Orders
            .Where(o => o.CreatedAt >= weekAgo && o.Status == OrderStatus.Completed)/**/
            .ToListAsync();

        var weekRevenue = weekOrders.Sum(o => o.TotalAmount);

        // Month stats
        var monthOrders = await context.Orders
            .Where(o => o.CreatedAt >= monthAgo && o.Status == OrderStatus.Completed)
            .ToListAsync();

        var monthRevenue = monthOrders.Sum(o => o.TotalAmount);

        // Active tables
        var activeTables = await context.CafeTables
            .CountAsync(t => t.Status == TableStatus.Occupied);

        // Low stock items
        var lowStockCount = await context.StoreProducts
            .CountAsync(sp => sp.CurrentStock < sp.MinimumStock);

        // Top products (last 30 days)
        var topProducts = await GetTopProductsAsync(30, 5);

        // Top waiters (last 30 days)
        var topWaiters = await GetTopWaitersAsync(30, 5);

        // Low stock products
        var lowStockProducts = await context.StoreProducts
            .Include(sp => sp.Store)
            .Where(sp => sp.CurrentStock < sp.MinimumStock)
            .Take(10)
            .ToListAsync();

        return new DashboardDto
        {
            TodayRevenue = todayRevenue,
            WeekRevenue = weekRevenue,
            MonthRevenue = monthRevenue,
            TodayOrders = todayOrders.Count,
            ActiveTables = activeTables,
            LowStockItems = lowStockCount,
            TopProducts = topProducts.ToList(),
            TopWaiters = topWaiters.ToList(),
            LowStockProducts = mapper.Map<List<StoreProductDto>>(lowStockProducts)
        };
    }

    public async Task<DailyStatisticsDto> GetDailyStatsAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var orders = await context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Category)
            .Where(o => o.CreatedAt >= startOfDay && o.CreatedAt < endOfDay)
            .ToListAsync();

        var completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();
        var totalRevenue = completedOrders.Sum(o => o.TotalAmount);

        // Top products
        var topProducts = completedOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.Product.Name })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Subtotal)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(5)
            .ToList();

        // Category sales
        var categorySales = completedOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.Product.CategoryId, i.Product.Category.Name })
            .Select(g => new CategorySalesDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Revenue = g.Sum(i => i.Subtotal),
                OrderCount = g.Count()
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        return new DailyStatisticsDto
        {
            Date = date,
            TotalRevenue = totalRevenue,
            TotalOrders = orders.Count,
            CompletedOrders = completedOrders.Count,
            CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled),
            AverageOrderValue = completedOrders.Any() ? totalRevenue / completedOrders.Count : 0,
            TopProducts = topProducts,
            CategorySales = categorySales
        };
    }

    public async Task<IEnumerable<WaiterPerformanceDto>> GetWaiterPerformanceAsync(int days = 30)
    {
        return await GetTopWaitersAsync(days, 100);
    }

    public async Task<RevenueChartDto> GetRevenueChartAsync(DateTime fromDate, DateTime toDate)
    {
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= fromDate && 
                       o.CreatedAt <= toDate && 
                       o.Status == OrderStatus.Completed)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new RevenueDataPointDto
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        return new RevenueChartDto { Data = orders };
    }

    // PRIVATE HELPERS

    private async Task<IEnumerable<TopProductDto>> GetTopProductsAsync(int days, int count)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        return await context.OrderItems
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => new { oi.ProductId, oi.Product.Name })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Subtotal)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(count)
            .ToListAsync();
    }

    private async Task<IEnumerable<WaiterPerformanceDto>> GetTopWaitersAsync(int days, int count)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        return await context.Orders
            .Include(o => o.Waiter)
            .Where(o => o.CreatedAt >= fromDate && o.Status == OrderStatus.Completed)
            .GroupBy(o => new { o.WaiterId, o.Waiter.FullName })
            .Select(g => new WaiterPerformanceDto
            {
                WaiterId = g.Key.WaiterId,
                WaiterName = g.Key.FullName,
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount),
                AverageOrderValue = g.Average(o => o.TotalAmount)
            })
            .OrderByDescending(w => w.TotalRevenue)
            .Take(count)
            .ToListAsync();
    }
}

