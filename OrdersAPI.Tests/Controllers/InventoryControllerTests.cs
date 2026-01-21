using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class InventoryControllerTests
{
    private readonly Mock<IInventoryService> _inventoryServiceMock;
    private readonly InventoryController _controller;
    private readonly Guid _testStoreProductId;
    private readonly Guid _testStoreId;

    public InventoryControllerTests()
    {
        _inventoryServiceMock = new Mock<IInventoryService>();
        _controller = new InventoryController(_inventoryServiceMock.Object);
        _testStoreProductId = Guid.NewGuid();
        _testStoreId = Guid.NewGuid();
    }

    #region GetStoreProducts Tests

    [Fact]
    public async Task GetStoreProducts_WithoutStoreId_ReturnsAllProducts()
    {
        // Arrange
        var expectedProducts = new List<StoreProductDto>
        {
            new StoreProductDto
            {
                Id = Guid.NewGuid(),
                StoreId = _testStoreId,
                StoreName = "Main Store",
                Name = "Tomatoes",
                Description = "Fresh tomatoes",
                PurchasePrice = 2.50m,
                CurrentStock = 100,
                MinimumStock = 20,
                Unit = "kg",
                IsLowStock = false,
                LastRestocked = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow
            },
            new StoreProductDto
            {
                Id = Guid.NewGuid(),
                StoreId = _testStoreId,
                StoreName = "Main Store",
                Name = "Cheese",
                Description = "Mozzarella",
                PurchasePrice = 8.99m,
                CurrentStock = 15,
                MinimumStock = 30,
                Unit = "kg",
                IsLowStock = true,
                LastRestocked = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow
            }
        };

        _inventoryServiceMock
            .Setup(x => x.GetAllStoreProductsAsync(null))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetStoreProducts(null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<StoreProductDto>;

        products.Should().NotBeNull();
        products.Should().HaveCount(2);
        products!.First().Name.Should().Be("Tomatoes");
        products.Last().IsLowStock.Should().BeTrue();

        _inventoryServiceMock.Verify(x => x.GetAllStoreProductsAsync(null), Times.Once);
    }

    [Fact]
    public async Task GetStoreProducts_WithStoreId_ReturnsFilteredProducts()
    {
        // Arrange
        var expectedProducts = new List<StoreProductDto>
        {
            new StoreProductDto
            {
                Id = Guid.NewGuid(),
                StoreId = _testStoreId,
                StoreName = "Main Store",
                Name = "Tomatoes",
                CurrentStock = 100,
                MinimumStock = 20,
                Unit = "kg",
                PurchasePrice = 2.50m,
                IsLowStock = false,
                LastRestocked = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }
        };

        _inventoryServiceMock
            .Setup(x => x.GetAllStoreProductsAsync(_testStoreId))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetStoreProducts(_testStoreId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<StoreProductDto>;

        products.Should().NotBeNull();
        products.Should().HaveCount(1);
        products!.First().StoreId.Should().Be(_testStoreId);

        _inventoryServiceMock.Verify(x => x.GetAllStoreProductsAsync(_testStoreId), Times.Once);
    }

    [Fact]
    public async Task GetStoreProducts_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        _inventoryServiceMock
            .Setup(x => x.GetAllStoreProductsAsync(null))
            .ReturnsAsync(new List<StoreProductDto>());

        // Act
        var result = await _controller.GetStoreProducts(null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<StoreProductDto>;

        products.Should().NotBeNull();
        products.Should().BeEmpty();

        _inventoryServiceMock.Verify(x => x.GetAllStoreProductsAsync(null), Times.Once);
    }

    #endregion

    #region GetStoreProduct Tests

    [Fact]
    public async Task GetStoreProduct_ExistingId_ReturnsProduct()
    {
        // Arrange
        var expectedProduct = new StoreProductDto
        {
            Id = _testStoreProductId,
            StoreId = _testStoreId,
            StoreName = "Main Store",
            Name = "Tomatoes",
            Description = "Fresh tomatoes",
            PurchasePrice = 2.50m,
            CurrentStock = 100,
            MinimumStock = 20,
            Unit = "kg",
            IsLowStock = false,
            LastRestocked = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _inventoryServiceMock
            .Setup(x => x.GetStoreProductByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _controller.GetStoreProduct(_testStoreProductId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var product = okResult!.Value as StoreProductDto;

        product.Should().NotBeNull();
        product!.Id.Should().Be(_testStoreProductId);
        product.Name.Should().Be("Tomatoes");
        product.CurrentStock.Should().Be(100);

        _inventoryServiceMock.Verify(x => x.GetStoreProductByIdAsync(_testStoreProductId), Times.Once);
    }

    [Fact]
    public async Task GetStoreProduct_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _inventoryServiceMock
            .Setup(x => x.GetStoreProductByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Store product with ID {_testStoreProductId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.GetStoreProduct(_testStoreProductId));

        _inventoryServiceMock.Verify(x => x.GetStoreProductByIdAsync(_testStoreProductId), Times.Once);
    }

    #endregion

    #region CreateStoreProduct Tests

    [Fact]
    public async Task CreateStoreProduct_ValidData_ReturnsCreatedProduct()
    {
        // Arrange
        var createDto = new CreateStoreProductDto
        {
            StoreId = _testStoreId,
            Name = "New Product",
            Description = "Test product",
            PurchasePrice = 5.99m,
            CurrentStock = 50,
            MinimumStock = 10,
            Unit = "pcs"
        };

        var expectedProduct = new StoreProductDto
        {
            Id = _testStoreProductId,
            StoreId = _testStoreId,
            StoreName = "Main Store",
            Name = "New Product",
            Description = "Test product",
            PurchasePrice = 5.99m,
            CurrentStock = 50,
            MinimumStock = 10,
            Unit = "pcs",
            IsLowStock = false,
            LastRestocked = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _inventoryServiceMock
            .Setup(x => x.CreateStoreProductAsync(It.IsAny<CreateStoreProductDto>()))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _controller.CreateStoreProduct(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var product = createdResult!.Value as StoreProductDto;

        product.Should().NotBeNull();
        product!.Name.Should().Be("New Product");
        product.PurchasePrice.Should().Be(5.99m);
        product.CurrentStock.Should().Be(50);

        createdResult.ActionName.Should().Be(nameof(_controller.GetStoreProduct));
        createdResult.RouteValues!["id"].Should().Be(_testStoreProductId);

        _inventoryServiceMock.Verify(x => x.CreateStoreProductAsync(It.IsAny<CreateStoreProductDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateStoreProduct_InvalidStoreId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var createDto = new CreateStoreProductDto
        {
            StoreId = Guid.NewGuid(),
            Name = "Product",
            PurchasePrice = 1.99m,
            Unit = "pcs"
        };

        _inventoryServiceMock
            .Setup(x => x.CreateStoreProductAsync(It.IsAny<CreateStoreProductDto>()))
            .ThrowsAsync(new KeyNotFoundException("Store not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.CreateStoreProduct(createDto));

        _inventoryServiceMock.Verify(x => x.CreateStoreProductAsync(It.IsAny<CreateStoreProductDto>()), Times.Once);
    }

    #endregion

    #region UpdateStoreProduct Tests

    [Fact]
    public async Task UpdateStoreProduct_ValidData_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateStoreProductDto
        {
            Name = "Updated Product",
            Description = "Updated description",
            PurchasePrice = 6.99m,
            MinimumStock = 15
        };

        _inventoryServiceMock
            .Setup(x => x.UpdateStoreProductAsync(It.IsAny<Guid>(), It.IsAny<UpdateStoreProductDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateStoreProduct(_testStoreProductId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _inventoryServiceMock.Verify(x => x.UpdateStoreProductAsync(_testStoreProductId, updateDto), Times.Once);
    }

    [Fact]
    public async Task UpdateStoreProduct_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateStoreProductDto
        {
            Name = "Updated Product"
        };

        _inventoryServiceMock
            .Setup(x => x.UpdateStoreProductAsync(It.IsAny<Guid>(), It.IsAny<UpdateStoreProductDto>()))
            .ThrowsAsync(new KeyNotFoundException($"Store product with ID {_testStoreProductId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.UpdateStoreProduct(_testStoreProductId, updateDto));

        _inventoryServiceMock.Verify(x => x.UpdateStoreProductAsync(_testStoreProductId, updateDto), Times.Once);
    }

    #endregion

    #region DeleteStoreProduct Tests

    [Fact]
    public async Task DeleteStoreProduct_ExistingId_ReturnsNoContent()
    {
        // Arrange
        _inventoryServiceMock
            .Setup(x => x.DeleteStoreProductAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteStoreProduct(_testStoreProductId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _inventoryServiceMock.Verify(x => x.DeleteStoreProductAsync(_testStoreProductId), Times.Once);
    }

    [Fact]
    public async Task DeleteStoreProduct_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _inventoryServiceMock
            .Setup(x => x.DeleteStoreProductAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Store product with ID {_testStoreProductId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.DeleteStoreProduct(_testStoreProductId));

        _inventoryServiceMock.Verify(x => x.DeleteStoreProductAsync(_testStoreProductId), Times.Once);
    }

    #endregion

    #region AdjustInventory Tests

    [Fact]
    public async Task AdjustInventory_IncreaseStock_ReturnsNoContent()
    {
        // Arrange
        var adjustDto = new AdjustInventoryDto
        {
            QuantityChange = 50,
            Type = "Restock",
            Reason = "New delivery"
        };

        _inventoryServiceMock
            .Setup(x => x.AdjustInventoryAsync(It.IsAny<Guid>(), It.IsAny<AdjustInventoryDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AdjustInventory(_testStoreProductId, adjustDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _inventoryServiceMock.Verify(x => x.AdjustInventoryAsync(_testStoreProductId, adjustDto), Times.Once);
    }

    [Fact]
    public async Task AdjustInventory_DecreaseStock_ReturnsNoContent()
    {
        // Arrange
        var adjustDto = new AdjustInventoryDto
        {
            QuantityChange = -20,
            Type = "Usage",
            Reason = "Prepared meals"
        };

        _inventoryServiceMock
            .Setup(x => x.AdjustInventoryAsync(It.IsAny<Guid>(), It.IsAny<AdjustInventoryDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AdjustInventory(_testStoreProductId, adjustDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _inventoryServiceMock.Verify(x => x.AdjustInventoryAsync(_testStoreProductId, adjustDto), Times.Once);
    }

    [Fact]
    public async Task AdjustInventory_InsufficientStock_ThrowsInvalidOperationException()
    {
        // Arrange
        var adjustDto = new AdjustInventoryDto
        {
            QuantityChange = -1000,
            Type = "Usage"
        };

        _inventoryServiceMock
            .Setup(x => x.AdjustInventoryAsync(It.IsAny<Guid>(), It.IsAny<AdjustInventoryDto>()))
            .ThrowsAsync(new InvalidOperationException("Insufficient stock"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.AdjustInventory(_testStoreProductId, adjustDto));

        _inventoryServiceMock.Verify(x => x.AdjustInventoryAsync(_testStoreProductId, adjustDto), Times.Once);
    }

    #endregion

    #region GetLowStockProducts Tests

    [Fact]
    public async Task GetLowStockProducts_ReturnsProductsBelowMinimum()
    {
        // Arrange
        var expectedProducts = new List<StoreProductDto>
        {
            new StoreProductDto
            {
                Id = Guid.NewGuid(),
                StoreId = _testStoreId,
                StoreName = "Main Store",
                Name = "Cheese",
                CurrentStock = 5,
                MinimumStock = 20,
                Unit = "kg",
                PurchasePrice = 8.99m,
                IsLowStock = true,
                LastRestocked = DateTime.UtcNow.AddDays(-10),
                CreatedAt = DateTime.UtcNow
            },
            new StoreProductDto
            {
                Id = Guid.NewGuid(),
                StoreId = _testStoreId,
                StoreName = "Main Store",
                Name = "Flour",
                CurrentStock = 8,
                MinimumStock = 25,
                Unit = "kg",
                PurchasePrice = 1.50m,
                IsLowStock = true,
                LastRestocked = DateTime.UtcNow.AddDays(-7),
                CreatedAt = DateTime.UtcNow
            }
        };

        _inventoryServiceMock
            .Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<StoreProductDto>;

        products.Should().NotBeNull();
        products.Should().HaveCount(2);
        products!.All(p => p.IsLowStock).Should().BeTrue();

        _inventoryServiceMock.Verify(x => x.GetLowStockProductsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetLowStockProducts_NoLowStock_ReturnsEmptyList()
    {
        // Arrange
        _inventoryServiceMock
            .Setup(x => x.GetLowStockProductsAsync())
            .ReturnsAsync(new List<StoreProductDto>());

        // Act
        var result = await _controller.GetLowStockProducts();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<StoreProductDto>;

        products.Should().NotBeNull();
        products.Should().BeEmpty();

        _inventoryServiceMock.Verify(x => x.GetLowStockProductsAsync(), Times.Once);
    }

    #endregion

    #region GetInventoryLogs Tests

    [Fact]
    public async Task GetInventoryLogs_WithoutFilters_ReturnsAllLogs()
    {
        // Arrange
        var expectedLogs = new List<InventoryLogDto>
        {
            new InventoryLogDto
            {
                Id = Guid.NewGuid(),
                StoreProductId = _testStoreProductId,
                StoreProductName = "Tomatoes",
                QuantityChange = 50,
                Type = "Restock",
                Reason = "New delivery",
                CreatedAt = DateTime.UtcNow
            },
            new InventoryLogDto
            {
                Id = Guid.NewGuid(),
                StoreProductId = _testStoreProductId,
                StoreProductName = "Tomatoes",
                QuantityChange = -10,
                Type = "Usage",
                Reason = "Pizza preparation",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            }
        };

        _inventoryServiceMock
            .Setup(x => x.GetInventoryLogsAsync(null, 30))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetInventoryLogs(null, 30);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var logs = okResult!.Value as IEnumerable<InventoryLogDto>;

        logs.Should().NotBeNull();
        logs.Should().HaveCount(2);
        logs!.First().Type.Should().Be("Restock");

        _inventoryServiceMock.Verify(x => x.GetInventoryLogsAsync(null, 30), Times.Once);
    }

    [Fact]
    public async Task GetInventoryLogs_WithStoreProductId_ReturnsFilteredLogs()
    {
        // Arrange
        var expectedLogs = new List<InventoryLogDto>
        {
            new InventoryLogDto
            {
                Id = Guid.NewGuid(),
                StoreProductId = _testStoreProductId,
                StoreProductName = "Tomatoes",
                QuantityChange = 50,
                Type = "Restock",
                CreatedAt = DateTime.UtcNow
            }
        };

        _inventoryServiceMock
            .Setup(x => x.GetInventoryLogsAsync(_testStoreProductId, 30))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetInventoryLogs(_testStoreProductId, 30);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var logs = okResult!.Value as IEnumerable<InventoryLogDto>;

        logs.Should().NotBeNull();
        logs.Should().HaveCount(1);
        logs!.First().StoreProductId.Should().Be(_testStoreProductId);

        _inventoryServiceMock.Verify(x => x.GetInventoryLogsAsync(_testStoreProductId, 30), Times.Once);
    }

    [Fact]
    public async Task GetInventoryLogs_CustomDays_ReturnsLogsForPeriod()
    {
        // Arrange
        var expectedLogs = new List<InventoryLogDto>();

        _inventoryServiceMock
            .Setup(x => x.GetInventoryLogsAsync(null, 7))
            .ReturnsAsync(expectedLogs);

        // Act
        var result = await _controller.GetInventoryLogs(null, 7);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();

        _inventoryServiceMock.Verify(x => x.GetInventoryLogsAsync(null, 7), Times.Once);
    }

    #endregion

    #region GetTotalStockValue Tests

    [Fact]
    public async Task GetTotalStockValue_WithoutStoreId_ReturnsTotalValue()
    {
        // Arrange
        var expectedValue = 15420.50m;

        _inventoryServiceMock
            .Setup(x => x.GetTotalStockValueAsync(null))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _controller.GetTotalStockValue(null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new 
        { 
            totalValue = expectedValue, 
            currency = "BAM" 
        });

        _inventoryServiceMock.Verify(x => x.GetTotalStockValueAsync(null), Times.Once);
    }

    [Fact]
    public async Task GetTotalStockValue_WithStoreId_ReturnsStoreValue()
    {
        // Arrange
        var expectedValue = 8250.00m;

        _inventoryServiceMock
            .Setup(x => x.GetTotalStockValueAsync(_testStoreId))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _controller.GetTotalStockValue(_testStoreId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new 
        { 
            totalValue = expectedValue, 
            currency = "BAM" 
        });

        _inventoryServiceMock.Verify(x => x.GetTotalStockValueAsync(_testStoreId), Times.Once);
    }

    #endregion

    #region GetConsumptionForecast Tests

    [Fact]
    public async Task GetConsumptionForecast_ReturnsForecasts()
    {
        // Arrange
        var expectedForecasts = new List<ConsumptionForecastDto>
        {
            new ConsumptionForecastDto
            {
                StoreProductId = _testStoreProductId,
                StoreProductName = "Tomatoes",
                CurrentStock = 50,
                AverageDailyConsumption = 5.5,
                EstimatedDaysUntilDepletion = 9,
                NeedsReorder = true,
                Unit = "kg"
            },
            new ConsumptionForecastDto
            {
                StoreProductId = Guid.NewGuid(),
                StoreProductName = "Cheese",
                CurrentStock = 100,
                AverageDailyConsumption = 2.3,
                EstimatedDaysUntilDepletion = 43,
                NeedsReorder = false,
                Unit = "kg"
            }
        };

        _inventoryServiceMock
            .Setup(x => x.GetConsumptionForecastAsync(30))
            .ReturnsAsync(expectedForecasts);

        // Act
        var result = await _controller.GetConsumptionForecast(30);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var forecasts = okResult!.Value as List<ConsumptionForecastDto>;

        forecasts.Should().NotBeNull();
        forecasts.Should().HaveCount(2);
        forecasts![0].NeedsReorder.Should().BeTrue();
        forecasts[1].NeedsReorder.Should().BeFalse();

        _inventoryServiceMock.Verify(x => x.GetConsumptionForecastAsync(30), Times.Once);
    }

    [Fact]
    public async Task GetConsumptionForecast_CustomDays_ReturnsForecasts()
    {
        // Arrange
        var expectedForecasts = new List<ConsumptionForecastDto>();

        _inventoryServiceMock
            .Setup(x => x.GetConsumptionForecastAsync(7))
            .ReturnsAsync(expectedForecasts);

        // Act
        var result = await _controller.GetConsumptionForecast(7);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();

        _inventoryServiceMock.Verify(x => x.GetConsumptionForecastAsync(7), Times.Once);
    }

    #endregion
}
