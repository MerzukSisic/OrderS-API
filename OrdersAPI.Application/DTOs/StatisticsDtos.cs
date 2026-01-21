namespace OrdersAPI.Application.DTOs;

public class DashboardDto
{
    public decimal TodayRevenue { get; set; }
    public decimal WeekRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int ActiveTables { get; set; }
    public int LowStockItems { get; set; }
    public decimal TodayVsYesterday { get; set; } // Percentage change
    public string TrendIndicator { get; set; } = string.Empty; // "up", "down", "neutral"
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<WaiterPerformanceDto> TopWaiters { get; set; } = new();
    public List<StoreProductDto> LowStockProducts { get; set; } = new();
}

public class DailyStatisticsDto
{
    public DateTime Date { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<CategorySalesDto> CategorySales { get; set; } = new();
}

// ✅ UPDATED: Dodato CategoryName polje
public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty; // ✅ NOVO POLJE
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class CategorySalesDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
    public decimal Percentage { get; set; }
}

public class WaiterPerformanceDto
{
    public Guid WaiterId { get; set; }
    public string WaiterName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class RevenueChartDto
{
    public List<RevenueDataPointDto> Data { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
}

public class RevenueDataPointDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class ProductSalesDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Percentage { get; set; }
}

public class PeakHourDto
{
    public int Hour { get; set; }
    public string TimeRange { get; set; } = string.Empty; // "14:00 - 15:00"
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}