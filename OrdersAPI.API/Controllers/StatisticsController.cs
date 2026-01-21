using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatisticsController(IStatisticsService statisticsService) : ControllerBase
{
    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var dashboard = await statisticsService.GetDashboardStatsAsync();
        return Ok(dashboard);
    }

    [HttpGet("daily")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DailyStatisticsDto>> GetDailyStats([FromQuery] DateTime? date = null)
    {
        var stats = await statisticsService.GetDailyStatsAsync(date ?? DateTime.UtcNow.Date);
        return Ok(stats);
    }

    [HttpGet("waiter-performance")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<WaiterPerformanceDto>>> GetWaiterPerformance([FromQuery] int days = 30)
    {
        var performance = await statisticsService.GetWaiterPerformanceAsync(days);
        return Ok(performance);
    }

    [HttpGet("revenue-chart")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RevenueChartDto>> GetRevenueChart(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var chart = await statisticsService.GetRevenueChartAsync(fromDate, toDate);
        return Ok(chart);
    }

    [HttpGet("top-selling-products")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ProductSalesDto>>> GetTopSellingProducts(
        [FromQuery] int count = 10,
        [FromQuery] int days = 30)
    {
        var products = await statisticsService.GetTopSellingProductsAsync(count, days);
        return Ok(products);
    }

    [HttpGet("peak-hours")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<PeakHourDto>>> GetPeakHours([FromQuery] int days = 7)
    {
        var peakHours = await statisticsService.GetPeakHoursAsync(days);
        return Ok(peakHours);
    }

    [HttpGet("category-sales")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<CategorySalesDto>>> GetCategorySales(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var categorySales = await statisticsService.GetCategorySalesAsync(fromDate, toDate);
        return Ok(categorySales);
    }
}