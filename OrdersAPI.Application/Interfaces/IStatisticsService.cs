using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IStatisticsService
{
    Task<DashboardDto> GetDashboardStatsAsync();
    Task<DailyStatisticsDto> GetDailyStatsAsync(DateTime date);
    Task<IEnumerable<WaiterPerformanceDto>> GetWaiterPerformanceAsync(int days);
    Task<RevenueChartDto> GetRevenueChartAsync(DateTime fromDate, DateTime toDate);
    
    Task<List<ProductSalesDto>> GetTopSellingProductsAsync(int count = 10, int days = 30);
    Task<List<PeakHourDto>> GetPeakHoursAsync(int days = 7);
    Task<List<CategorySalesDto>> GetCategorySalesAsync(DateTime fromDate, DateTime toDate);
}
