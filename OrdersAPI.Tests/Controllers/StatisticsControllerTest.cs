using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using System.Security.Claims;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class StatisticsControllerTests
{
    private readonly Mock<IStatisticsService> _statisticsServiceMock;
    private readonly StatisticsController _controller;

    public StatisticsControllerTests()
    {
        _statisticsServiceMock = new Mock<IStatisticsService>();
        _controller = new StatisticsController(_statisticsServiceMock.Object);

        SetupAuthenticatedUser();
    }

    #region GetDashboard Tests

    [Fact]
    public async Task GetDashboard_ReturnsCompleteDashboardStats()
    {
        // Arrange
        var expectedDashboard = new DashboardDto
        {
            TodayRevenue = 2500.00m,
            WeekRevenue = 15000.00m,
            MonthRevenue = 45000.00m,
            TodayOrders = 45,
            ActiveTables = 8,
            LowStockItems = 3,
            TodayVsYesterday = 15.5m,
            TrendIndicator = "up",
            TopProducts = new List<TopProductDto>
            {
                new TopProductDto
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Pizza Margherita",
                    QuantitySold = 25,
                    Revenue = 312.50m
                }
            },
            TopWaiters = new List<WaiterPerformanceDto>
            {
                new WaiterPerformanceDto
                {
                    WaiterId = Guid.NewGuid(),
                    WaiterName = "John Doe",
                    TotalOrders = 20,
                    TotalRevenue = 800.00m,
                    AverageOrderValue = 40.00m
                }
            },
            LowStockProducts = new List<StoreProductDto>()
        };

        _statisticsServiceMock
            .Setup(x => x.GetDashboardStatsAsync())
            .ReturnsAsync(expectedDashboard);

        // Act
        var result = await _controller.GetDashboard();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var dashboard = okResult!.Value as DashboardDto;

        dashboard.Should().NotBeNull();
        dashboard!.TodayRevenue.Should().Be(2500.00m);
        dashboard.WeekRevenue.Should().Be(15000.00m);
        dashboard.MonthRevenue.Should().Be(45000.00m);
        dashboard.TodayOrders.Should().Be(45);
        dashboard.ActiveTables.Should().Be(8);
        dashboard.LowStockItems.Should().Be(3);
        dashboard.TodayVsYesterday.Should().Be(15.5m);
        dashboard.TrendIndicator.Should().Be("up");
        dashboard.TopProducts.Should().HaveCount(1);
        dashboard.TopWaiters.Should().HaveCount(1);

        _statisticsServiceMock.Verify(x => x.GetDashboardStatsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetDashboard_NegativeTrend_ReturnsDashboardWithDownIndicator()
    {
        // Arrange
        var expectedDashboard = new DashboardDto
        {
            TodayRevenue = 1500.00m,
            WeekRevenue = 10000.00m,
            MonthRevenue = 30000.00m,
            TodayOrders = 25,
            ActiveTables = 5,
            LowStockItems = 0,
            TodayVsYesterday = -10.5m,
            TrendIndicator = "down",
            TopProducts = new List<TopProductDto>(),
            TopWaiters = new List<WaiterPerformanceDto>(),
            LowStockProducts = new List<StoreProductDto>()
        };

        _statisticsServiceMock
            .Setup(x => x.GetDashboardStatsAsync())
            .ReturnsAsync(expectedDashboard);

        // Act
        var result = await _controller.GetDashboard();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var dashboard = okResult!.Value as DashboardDto;

        dashboard.Should().NotBeNull();
        dashboard!.TodayVsYesterday.Should().Be(-10.5m);
        dashboard.TrendIndicator.Should().Be("down");

        _statisticsServiceMock.Verify(x => x.GetDashboardStatsAsync(), Times.Once);
    }

    #endregion

    #region GetDailyStats Tests

    [Fact]
    public async Task GetDailyStats_WithoutDate_ReturnsTodayStats()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var expectedStats = new DailyStatisticsDto
        {
            Date = today,
            TotalRevenue = 2500.00m,
            TotalOrders = 45,
            CompletedOrders = 42,
            CancelledOrders = 3,
            AverageOrderValue = 55.56m,
            TopProducts = new List<TopProductDto>(),
            CategorySales = new List<CategorySalesDto>()
        };

        _statisticsServiceMock
            .Setup(x => x.GetDailyStatsAsync(today))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetDailyStats(null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var stats = okResult!.Value as DailyStatisticsDto;

        stats.Should().NotBeNull();
        stats!.Date.Should().Be(today);
        stats.TotalRevenue.Should().Be(2500.00m);
        stats.TotalOrders.Should().Be(45);
        stats.CompletedOrders.Should().Be(42);
        stats.CancelledOrders.Should().Be(3);
        stats.AverageOrderValue.Should().Be(55.56m);

        _statisticsServiceMock.Verify(x => x.GetDailyStatsAsync(today), Times.Once);
    }

    [Fact]
    public async Task GetDailyStats_WithSpecificDate_ReturnsStatsForThatDate()
    {
        // Arrange
        var specificDate = new DateTime(2026, 1, 5);
        var expectedStats = new DailyStatisticsDto
        {
            Date = specificDate,
            TotalRevenue = 3200.00m,
            TotalOrders = 52,
            CompletedOrders = 50,
            CancelledOrders = 2,
            AverageOrderValue = 61.54m,
            TopProducts = new List<TopProductDto>
            {
                new TopProductDto
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Pizza Margherita",
                    QuantitySold = 30,
                    Revenue = 375.00m
                }
            },
            CategorySales = new List<CategorySalesDto>
            {
                new CategorySalesDto
                {
                    CategoryId = Guid.NewGuid(),
                    CategoryName = "Pizza",
                    Revenue = 1200.00m,
                    OrderCount = 25,
                    Percentage = 37.5m
                }
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetDailyStatsAsync(specificDate))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetDailyStats(specificDate);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var stats = okResult!.Value as DailyStatisticsDto;

        stats.Should().NotBeNull();
        stats!.Date.Should().Be(specificDate);
        stats.TopProducts.Should().HaveCount(1);
        stats.CategorySales.Should().HaveCount(1);

        _statisticsServiceMock.Verify(x => x.GetDailyStatsAsync(specificDate), Times.Once);
    }

    #endregion

    #region GetWaiterPerformance Tests

    [Fact]
    public async Task GetWaiterPerformance_Default30Days_ReturnsWaiterStats()
    {
        // Arrange
        var expectedPerformance = new List<WaiterPerformanceDto>
        {
            new WaiterPerformanceDto
            {
                WaiterId = Guid.NewGuid(),
                WaiterName = "John Doe",
                TotalOrders = 150,
                TotalRevenue = 7500.00m,
                AverageOrderValue = 50.00m
            },
            new WaiterPerformanceDto
            {
                WaiterId = Guid.NewGuid(),
                WaiterName = "Jane Smith",
                TotalOrders = 120,
                TotalRevenue = 6000.00m,
                AverageOrderValue = 50.00m
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetWaiterPerformanceAsync(30))
            .ReturnsAsync(expectedPerformance);

        // Act
        var result = await _controller.GetWaiterPerformance(30);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var performance = okResult!.Value as IEnumerable<WaiterPerformanceDto>;

        performance.Should().NotBeNull();
        performance.Should().HaveCount(2);
        performance!.First().WaiterName.Should().Be("John Doe");
        performance.First().TotalOrders.Should().Be(150);

        _statisticsServiceMock.Verify(x => x.GetWaiterPerformanceAsync(30), Times.Once);
    }

    [Fact]
    public async Task GetWaiterPerformance_Custom7Days_ReturnsWeeklyStats()
    {
        // Arrange
        var expectedPerformance = new List<WaiterPerformanceDto>
        {
            new WaiterPerformanceDto
            {
                WaiterId = Guid.NewGuid(),
                WaiterName = "Bob Wilson",
                TotalOrders = 35,
                TotalRevenue = 1750.00m,
                AverageOrderValue = 50.00m
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetWaiterPerformanceAsync(7))
            .ReturnsAsync(expectedPerformance);

        // Act
        var result = await _controller.GetWaiterPerformance(7);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var performance = okResult!.Value as IEnumerable<WaiterPerformanceDto>;

        performance.Should().NotBeNull();
        performance.Should().HaveCount(1);

        _statisticsServiceMock.Verify(x => x.GetWaiterPerformanceAsync(7), Times.Once);
    }

    #endregion

    #region GetRevenueChart Tests

    [Fact]
    public async Task GetRevenueChart_DateRange_ReturnsRevenueData()
    {
        // Arrange
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 1, 7);

        var expectedChart = new RevenueChartDto
        {
            TotalRevenue = 15000.00m,
            TotalOrders = 300,
            Data = new List<RevenueDataPointDto>
            {
                new RevenueDataPointDto
                {
                    Date = new DateTime(2026, 1, 1),
                    Revenue = 2000.00m,
                    OrderCount = 40
                },
                new RevenueDataPointDto
                {
                    Date = new DateTime(2026, 1, 2),
                    Revenue = 2200.00m,
                    OrderCount = 45
                },
                new RevenueDataPointDto
                {
                    Date = new DateTime(2026, 1, 3),
                    Revenue = 2100.00m,
                    OrderCount = 42
                }
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetRevenueChartAsync(fromDate, toDate))
            .ReturnsAsync(expectedChart);

        // Act
        var result = await _controller.GetRevenueChart(fromDate, toDate);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var chart = okResult!.Value as RevenueChartDto;

        chart.Should().NotBeNull();
        chart!.TotalRevenue.Should().Be(15000.00m);
        chart.TotalOrders.Should().Be(300);
        chart.Data.Should().HaveCount(3);
        chart.Data[0].Revenue.Should().Be(2000.00m);
        chart.Data[0].OrderCount.Should().Be(40);

        _statisticsServiceMock.Verify(x => x.GetRevenueChartAsync(fromDate, toDate), Times.Once);
    }

    [Fact]
    public async Task GetRevenueChart_SingleDay_ReturnsOneDayData()
    {
        // Arrange
        var singleDate = new DateTime(2026, 1, 7);

        var expectedChart = new RevenueChartDto
        {
            TotalRevenue = 2500.00m,
            TotalOrders = 45,
            Data = new List<RevenueDataPointDto>
            {
                new RevenueDataPointDto
                {
                    Date = singleDate,
                    Revenue = 2500.00m,
                    OrderCount = 45
                }
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetRevenueChartAsync(singleDate, singleDate))
            .ReturnsAsync(expectedChart);

        // Act
        var result = await _controller.GetRevenueChart(singleDate, singleDate);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var chart = okResult!.Value as RevenueChartDto;

        chart.Should().NotBeNull();
        chart!.Data.Should().HaveCount(1);

        _statisticsServiceMock.Verify(x => x.GetRevenueChartAsync(singleDate, singleDate), Times.Once);
    }

    #endregion

    #region GetTopSellingProducts Tests

    [Fact]
    public async Task GetTopSellingProducts_Default10Items30Days_ReturnsTopProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductSalesDto>
        {
            new ProductSalesDto
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Pizza Margherita",
                CategoryName = "Pizza",
                QuantitySold = 150,
                Revenue = 1875.00m,
                Percentage = 25.5m
            },
            new ProductSalesDto
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Coca Cola",
                CategoryName = "Drinks",
                QuantitySold = 200,
                Revenue = 500.00m,
                Percentage = 20.0m
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetTopSellingProductsAsync(10, 30))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetTopSellingProducts(10, 30);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as List<ProductSalesDto>;

        products.Should().NotBeNull();
        products.Should().HaveCount(2);
        products![0].ProductName.Should().Be("Pizza Margherita");
        products[0].QuantitySold.Should().Be(150);
        products[0].Revenue.Should().Be(1875.00m);

        _statisticsServiceMock.Verify(x => x.GetTopSellingProductsAsync(10, 30), Times.Once);
    }

    [Fact]
    public async Task GetTopSellingProducts_Top5Last7Days_ReturnsTopFiveProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductSalesDto>
        {
            new ProductSalesDto
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Pizza Pepperoni",
                CategoryName = "Pizza",
                QuantitySold = 45,
                Revenue = 630.00m,
                Percentage = 30.0m
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetTopSellingProductsAsync(5, 7))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetTopSellingProducts(5, 7);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as List<ProductSalesDto>;

        products.Should().NotBeNull();
        products.Should().HaveCount(1);

        _statisticsServiceMock.Verify(x => x.GetTopSellingProductsAsync(5, 7), Times.Once);
    }

    #endregion

    #region GetPeakHours Tests

    [Fact]
    public async Task GetPeakHours_Default7Days_ReturnsPeakHourStats()
    {
        // Arrange
        var expectedPeakHours = new List<PeakHourDto>
        {
            new PeakHourDto
            {
                Hour = 12,
                TimeRange = "12:00 - 13:00",
                OrderCount = 85,
                Revenue = 4250.00m,
                AverageOrderValue = 50.00m
            },
            new PeakHourDto
            {
                Hour = 19,
                TimeRange = "19:00 - 20:00",
                OrderCount = 95,
                Revenue = 5225.00m,
                AverageOrderValue = 55.00m
            },
            new PeakHourDto
            {
                Hour = 20,
                TimeRange = "20:00 - 21:00",
                OrderCount = 90,
                Revenue = 4950.00m,
                AverageOrderValue = 55.00m
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetPeakHoursAsync(7))
            .ReturnsAsync(expectedPeakHours);

        // Act
        var result = await _controller.GetPeakHours(7);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var peakHours = okResult!.Value as List<PeakHourDto>;

        peakHours.Should().NotBeNull();
        peakHours.Should().HaveCount(3);
        peakHours![1].Hour.Should().Be(19);
        peakHours[1].OrderCount.Should().Be(95);
        peakHours[1].Revenue.Should().Be(5225.00m);

        _statisticsServiceMock.Verify(x => x.GetPeakHoursAsync(7), Times.Once);
    }

    [Fact]
    public async Task GetPeakHours_Custom30Days_ReturnsMonthlyPeakHours()
    {
        // Arrange
        var expectedPeakHours = new List<PeakHourDto>
        {
            new PeakHourDto
            {
                Hour = 13,
                TimeRange = "13:00 - 14:00",
                OrderCount = 320,
                Revenue = 16000.00m,
                AverageOrderValue = 50.00m
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetPeakHoursAsync(30))
            .ReturnsAsync(expectedPeakHours);

        // Act
        var result = await _controller.GetPeakHours(30);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var peakHours = okResult!.Value as List<PeakHourDto>;

        peakHours.Should().NotBeNull();
        peakHours.Should().HaveCount(1);

        _statisticsServiceMock.Verify(x => x.GetPeakHoursAsync(30), Times.Once);
    }

    #endregion

    #region GetCategorySales Tests

    [Fact]
    public async Task GetCategorySales_DateRange_ReturnsCategorySalesData()
    {
        // Arrange
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 1, 7);

        var expectedCategorySales = new List<CategorySalesDto>
        {
            new CategorySalesDto
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = "Pizza",
                Revenue = 5000.00m,
                OrderCount = 120,
                Percentage = 40.0m
            },
            new CategorySalesDto
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = "Drinks",
                Revenue = 2500.00m,
                OrderCount = 180,
                Percentage = 20.0m
            },
            new CategorySalesDto
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = "Pasta",
                Revenue = 3500.00m,
                OrderCount = 100,
                Percentage = 28.0m
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetCategorySalesAsync(fromDate, toDate))
            .ReturnsAsync(expectedCategorySales);

        // Act
        var result = await _controller.GetCategorySales(fromDate, toDate);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var categorySales = okResult!.Value as List<CategorySalesDto>;

        categorySales.Should().NotBeNull();
        categorySales.Should().HaveCount(3);
        categorySales![0].CategoryName.Should().Be("Pizza");
        categorySales[0].Revenue.Should().Be(5000.00m);
        categorySales[0].OrderCount.Should().Be(120);
        categorySales[0].Percentage.Should().Be(40.0m);

        _statisticsServiceMock.Verify(x => x.GetCategorySalesAsync(fromDate, toDate), Times.Once);
    }

    [Fact]
    public async Task GetCategorySales_SingleDay_ReturnsOneDayCategorySales()
    {
        // Arrange
        var singleDate = new DateTime(2026, 1, 7);

        var expectedCategorySales = new List<CategorySalesDto>
        {
            new CategorySalesDto
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = "Pizza",
                Revenue = 800.00m,
                OrderCount = 20,
                Percentage = 50.0m
            },
            new CategorySalesDto
            {
                CategoryId = Guid.NewGuid(),
                CategoryName = "Drinks",
                Revenue = 400.00m,
                OrderCount = 30,
                Percentage = 25.0m
            }
        };

        _statisticsServiceMock
            .Setup(x => x.GetCategorySalesAsync(singleDate, singleDate))
            .ReturnsAsync(expectedCategorySales);

        // Act
        var result = await _controller.GetCategorySales(singleDate, singleDate);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var categorySales = okResult!.Value as List<CategorySalesDto>;

        categorySales.Should().NotBeNull();
        categorySales.Should().HaveCount(2);

        _statisticsServiceMock.Verify(x => x.GetCategorySalesAsync(singleDate, singleDate), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "admin@example.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}
