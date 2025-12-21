namespace OrdersAPI.Application.DTOs;

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

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class CategorySalesDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class WaiterPerformanceDto
{
    public Guid WaiterId { get; set; }
    public string WaiterName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class DashboardDto
{
    public decimal TodayRevenue { get; set; }
    public decimal WeekRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int ActiveTables { get; set; }
    public int LowStockItems { get; set; }
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<WaiterPerformanceDto> TopWaiters { get; set; } = new();
    public List<StoreProductDto> LowStockProducts { get; set; } = new();
}

public class RevenueChartDto
{
    public List<RevenueDataPointDto> Data { get; set; } = new();
}

public class RevenueDataPointDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}
