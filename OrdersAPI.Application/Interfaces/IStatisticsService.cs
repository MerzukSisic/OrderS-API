using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IStatisticsService
{
    Task<DashboardDto> GetDashboardStatsAsync();
    Task<DailyStatisticsDto> GetDailyStatsAsync(DateTime date);
    Task<IEnumerable<WaiterPerformanceDto>> GetWaiterPerformanceAsync(int days);
    Task<RevenueChartDto> GetRevenueChartAsync(DateTime fromDate, DateTime toDate);
}
