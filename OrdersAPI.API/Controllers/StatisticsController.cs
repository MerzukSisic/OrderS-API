using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(IStatisticsService statisticsService, ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var dashboard = await _statisticsService.GetDashboardStatsAsync();
        return Ok(dashboard);
    }

    [HttpGet("daily")]
    public async Task<ActionResult<DailyStatisticsDto>> GetDailyStats([FromQuery] DateTime? date = null)
    {
        var stats = await _statisticsService.GetDailyStatsAsync(date ?? DateTime.Today);
        return Ok(stats);
    }

    [HttpGet("waiter-performance")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<WaiterPerformanceDto>>> GetWaiterPerformance([FromQuery] int days = 30)
    {
        var performance = await _statisticsService.GetWaiterPerformanceAsync(days);
        return Ok(performance);
    }

    [HttpGet("revenue-chart")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RevenueChartDto>> GetRevenueChart(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var chart = await _statisticsService.GetRevenueChartAsync(fromDate, toDate);
        return Ok(chart);
    }
}
