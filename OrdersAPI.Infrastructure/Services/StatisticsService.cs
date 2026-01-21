using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class StatisticsService(
    ApplicationDbContext context,
    ILogger<StatisticsService> logger) : IStatisticsService
{
    public async Task<DashboardDto> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var yesterday = today.AddDays(-1);
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddMonths(-1);

        // Single query for orders
        var allOrders = await context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= monthAgo && o.Status == OrderStatus.Completed)
            .Select(o => new { o.TotalAmount, o.CreatedAt })
            .ToListAsync();

        // Calculate stats from memory
        var todayRevenue = allOrders.Where(o => o.CreatedAt >= today).Sum(o => o.TotalAmount);
        var yesterdayRevenue = allOrders.Where(o => o.CreatedAt >= yesterday && o.CreatedAt < today).Sum(o => o.TotalAmount);
        var weekRevenue = allOrders.Where(o => o.CreatedAt >= weekAgo).Sum(o => o.TotalAmount);
        var monthRevenue = allOrders.Sum(o => o.TotalAmount);
        var todayOrders = allOrders.Count(o => o.CreatedAt >= today);

        // Calculate trend
        var percentageChange = yesterdayRevenue > 0 
            ? ((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100 
            : 0;

        var trendIndicator = percentageChange > 5 ? "up" 
            : percentageChange < -5 ? "down" 
            : "neutral";

        // Active tables
        var activeTables = await context.CafeTables
            .AsNoTracking()
            .CountAsync(t => t.Status == TableStatus.Occupied);

        // Low stock count
        var lowStockCount = await context.StoreProducts
            .AsNoTracking()
            .CountAsync(sp => sp.CurrentStock < sp.MinimumStock);

        // ✅ ISPRAVLJENO: Zovi helper metodu koja vraća TopProductDto
        var topProducts = await GetTopProductsAsync(5, 30);

        // Top waiters
        var topWaiters = await GetTopWaitersAsync(30, 5);

        // Low stock products
        var lowStockProducts = await context.StoreProducts
            .AsNoTracking()
            .Include(sp => sp.Store)
            .Where(sp => sp.CurrentStock < sp.MinimumStock)
            .OrderBy(sp => sp.CurrentStock)
            .Take(10)
            .Select(sp => new StoreProductDto
            {
                Id = sp.Id,
                Name = sp.Name,
                CurrentStock = sp.CurrentStock,
                MinimumStock = sp.MinimumStock,
                Unit = sp.Unit.ToString(),
                StoreId = sp.StoreId,
                StoreName = sp.Store.Name,
                PurchasePrice = sp.PurchasePrice,
                LastRestocked = sp.LastRestocked
            })
            .ToListAsync();

        logger.LogInformation("Dashboard stats generated: Today={TodayRevenue} KM, Orders={TodayOrders}", 
            todayRevenue, todayOrders);

        return new DashboardDto
        {
            TodayRevenue = todayRevenue,
            WeekRevenue = weekRevenue,
            MonthRevenue = monthRevenue,
            TodayOrders = todayOrders,
            ActiveTables = activeTables,
            LowStockItems = lowStockCount,
            TodayVsYesterday = percentageChange,
            TrendIndicator = trendIndicator,
            TopProducts = topProducts, // ✅ Already List<TopProductDto>
            TopWaiters = topWaiters.ToList(),
            LowStockProducts = lowStockProducts
        };
    }

    public async Task<DailyStatisticsDto> GetDailyStatsAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var orders = await context.Orders
            .AsNoTracking()
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
                OrderCount = g.Count(),
                Percentage = 0
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        // Calculate percentages
        foreach (var category in categorySales)
        {
            category.Percentage = totalRevenue > 0 ? (category.Revenue / totalRevenue) * 100 : 0;
        }

        logger.LogInformation("Daily stats for {Date}: Revenue={Revenue} KM, Orders={Orders}", 
            date, totalRevenue, completedOrders.Count);

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

    public async Task<IEnumerable<WaiterPerformanceDto>> GetWaiterPerformanceAsync(int days)
    {
        return await GetTopWaitersAsync(days, 100);
    }

    public async Task<RevenueChartDto> GetRevenueChartAsync(DateTime fromDate, DateTime toDate)
    {
        var data = await context.Orders
            .AsNoTracking()
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

        logger.LogInformation("Revenue chart generated from {FromDate} to {ToDate} with {DataPoints} points", 
            fromDate, toDate, data.Count);

        return new RevenueChartDto 
        { 
            Data = data,
            TotalRevenue = data.Sum(d => d.Revenue),
            TotalOrders = data.Sum(d => d.OrderCount)
        };
    }

    public async Task<List<ProductSalesDto>> GetTopSellingProductsAsync(int count = 10, int days = 30)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        // ✅ ISPRAVLJENO: Explicit names za properties
        var products = await context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
            .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => new 
            { 
                oi.ProductId, 
                ProductName = oi.Product.Name, 
                CategoryName = oi.Product.Category.Name 
            })
            .Select(g => new ProductSalesDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName, // ✅ Explicit name
                CategoryName = g.Key.CategoryName, // ✅ Explicit name
                QuantitySold = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Subtotal),
                Percentage = 0
            })
            .OrderByDescending(p => p.Revenue)
            .Take(count)
            .ToListAsync();

        var totalRevenue = products.Sum(p => p.Revenue);
        foreach (var product in products)
        {
            product.Percentage = totalRevenue > 0 ? (product.Revenue / totalRevenue) * 100 : 0;
        }

        logger.LogInformation("Top {Count} selling products retrieved for last {Days} days", count, days);

        return products;
    }

    public async Task<List<PeakHourDto>> GetPeakHoursAsync(int days = 7)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        // ✅ ISPRAVLJENO: Load data prvo, formatiranje after
        var peakHoursData = await context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= fromDate && o.Status == OrderStatus.Completed)
            .GroupBy(o => o.CreatedAt.Hour)
            .Select(g => new
            {
                Hour = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount),
                AverageOrderValue = g.Average(o => o.TotalAmount)
            })
            .OrderByDescending(p => p.OrderCount)
            .ToListAsync();

        // Map to DTO sa formatiranjem
        var peakHours = peakHoursData.Select(p => new PeakHourDto
        {
            Hour = p.Hour,
            TimeRange = FormatTimeRange(p.Hour), // ✅ Now can call method
            OrderCount = p.OrderCount,
            Revenue = p.Revenue,
            AverageOrderValue = p.AverageOrderValue
        }).ToList();

        logger.LogInformation("Peak hours analysis completed for last {Days} days", days);

        return peakHours;
    }

    public async Task<List<CategorySalesDto>> GetCategorySalesAsync(DateTime fromDate, DateTime toDate)
    {
        var categorySales = await context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
            .Where(oi => oi.Order.CreatedAt >= fromDate && 
                        oi.Order.CreatedAt <= toDate && 
                        oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => new { oi.Product.CategoryId, oi.Product.Category.Name })
            .Select(g => new CategorySalesDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Revenue = g.Sum(oi => oi.Subtotal),
                OrderCount = g.Count(),
                Percentage = 0
            })
            .OrderByDescending(c => c.Revenue)
            .ToListAsync();

        var totalRevenue = categorySales.Sum(c => c.Revenue);
        foreach (var category in categorySales)
        {
            category.Percentage = totalRevenue > 0 ? (category.Revenue / totalRevenue) * 100 : 0;
        }

        logger.LogInformation("Category sales from {FromDate} to {ToDate}: {Count} categories", 
            fromDate, toDate, categorySales.Count);

        return categorySales;
    }

    // ========== PRIVATE HELPER METHODS ==========

    // ✅ NOVA helper metoda za Dashboard
    // ✅ UPDATED helper metoda za Dashboard - Uključuje CategoryName
    private async Task<List<TopProductDto>> GetTopProductsAsync(int count, int days)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);

        return await context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Order)
            .Include(oi => oi.Product)
            .ThenInclude(p => p.Category) // ✅ DODAJ Category Include
            .Where(oi => oi.Order.CreatedAt >= fromDate && oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => new 
            { 
                oi.ProductId, 
                ProductName = oi.Product.Name,
                CategoryName = oi.Product.Category.Name // ✅ DODAJ CategoryName u GroupBy
            })
            .Select(g => new TopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                CategoryName = g.Key.CategoryName, // ✅ MAPAJ CategoryName
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
            .AsNoTracking()
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

    private static string FormatTimeRange(int hour)
    {
        return $"{hour:D2}:00 - {hour + 1:D2}:00";
    }
}