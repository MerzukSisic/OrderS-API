using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Tests.Helpers;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class InventoryControllerTests : TestBase
{
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly Mock<ILogger<InventoryController>> _loggerMock;
    private readonly InventoryController _controller;

    public InventoryControllerTests()
    {
        _inventoryServiceMock = new Mock<IInventoryService>();
        _loggerMock = new Mock<ILogger<InventoryController>>();
        _controller = new InventoryController(_inventoryServiceMock.Object, _loggerMock.Object);
        _controller.ControllerContext = CreateControllerContext(Guid.NewGuid());
    }

    [Fact]
    public async Task GetStoreProducts_ReturnsOkWithProducts()
    {
        // Arrange
        var products = new List<StoreProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Coffee Beans", CurrentStock = 100 },
            new() { Id = Guid.NewGuid(), Name = "Milk", CurrentStock = 50 }
        };

        _inventoryServiceMock.Setup(x => x.GetAllStoreProductsAsync(null))
            .ReturnsAsync(products);

        // Act
        var result = await _controller.GetStoreProducts();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducts = okResult.Value.Should().BeAssignableTo<IEnumerable<StoreProductDto>>().Subject;
        returnedProducts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLowStockProducts_ReturnsOnlyLowStockItems()
    {
        // Arrange
        var lowStockProducts = new List<StoreProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Sugar", CurrentStock = 5, MinimumStock = 10, IsLowStock = true }
        };

        _inventoryServiceMock.Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync(lowStockProducts);

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducts = okResult.Value.Should().BeAssignableTo<IEnumerable<StoreProductDto>>().Subject;
        returnedProducts.Should().AllSatisfy(p => p.IsLowStock.Should().BeTrue());
    }

    [Fact]
    public async Task AdjustInventory_ValidData_ReturnsNoContent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var adjustDto = new AdjustInventoryDto
        {
            QuantityChange = 50,
            Type = "Restock",
            Reason = "Weekly restock"
        };

        _inventoryServiceMock.Setup(x => x.AdjustInventoryAsync(productId, adjustDto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AdjustInventory(productId, adjustDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
