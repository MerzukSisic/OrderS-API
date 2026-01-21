using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Enums;
using System.Security.Claims;
using OrdersAPI.Domain.Entities;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly OrdersController _controller;
    private readonly Guid _testUserId;
    private readonly Guid _testOrderId;
    private readonly Guid _testTableId;
    private readonly Guid _testOrderItemId;

    public OrdersControllerTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _controller = new OrdersController(_orderServiceMock.Object);
        _testUserId = Guid.NewGuid();
        _testOrderId = Guid.NewGuid();
        _testTableId = Guid.NewGuid();
        _testOrderItemId = Guid.NewGuid();

        SetupAuthenticatedUser(_testUserId);
    }

    #region CreateOrder Tests

    [Fact]
    public async Task CreateOrder_ValidData_ReturnsCreatedOrder()
    {
        // Arrange
        var createDto = new CreateOrderDto
        {
            TableId = _testTableId,
            Type = "DineIn",
            IsPartnerOrder = false,
            Notes = "No onions",
            Items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2,
                    Notes = "Extra cheese",
                    SelectedAccompanimentIds = new List<Guid>()
                }
            }
        };

        var expectedOrder = new OrderDto
        {
            Id = _testOrderId,
            WaiterId = _testUserId,
            WaiterName = "Test Waiter",
            TableId = _testTableId,
            TableNumber = "5",
            Status = "Pending",
            Type = "DineIn",
            IsPartnerOrder = false,
            TotalAmount = 25.50m,
            Notes = "No onions",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItemDto>()
        };

        _orderServiceMock
            .Setup(x => x.CreateOrderAsync(_testUserId, It.IsAny<CreateOrderDto>()))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _controller.CreateOrder(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var order = createdResult!.Value as OrderDto;

        order.Should().NotBeNull();
        order!.Id.Should().Be(_testOrderId);
        order.WaiterId.Should().Be(_testUserId);
        order.TableId.Should().Be(_testTableId);
        order.Status.Should().Be("Pending");
        order.TotalAmount.Should().Be(25.50m);

        createdResult.ActionName.Should().Be(nameof(_controller.GetOrder));
        createdResult.RouteValues!["id"].Should().Be(_testOrderId);

        _orderServiceMock.Verify(x => x.CreateOrderAsync(_testUserId, It.IsAny<CreateOrderDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_InvalidTableId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var createDto = new CreateOrderDto
        {
            TableId = Guid.NewGuid(),
            Type = "DineIn",
            Items = new List<CreateOrderItemDto>()
        };

        _orderServiceMock
            .Setup(x => x.CreateOrderAsync(_testUserId, It.IsAny<CreateOrderDto>()))
            .ThrowsAsync(new KeyNotFoundException("Table not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.CreateOrder(createDto));

        _orderServiceMock.Verify(x => x.CreateOrderAsync(_testUserId, It.IsAny<CreateOrderDto>()), Times.Once);
    }

    #endregion

    #region GetOrder Tests

    [Fact]
    public async Task GetOrder_ExistingId_ReturnsOrder()
    {
        // Arrange
        var expectedOrder = new OrderDto
        {
            Id = _testOrderId,
            WaiterId = _testUserId,
            WaiterName = "Test Waiter",
            TableId = _testTableId,
            TableNumber = "5",
            Status = "Pending",
            Type = "DineIn",
            TotalAmount = 25.50m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = new List<OrderItemDto>()
        };

        _orderServiceMock
            .Setup(x => x.GetOrderByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _controller.GetOrder(_testOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var order = okResult!.Value as OrderDto;

        order.Should().NotBeNull();
        order!.Id.Should().Be(_testOrderId);
        order.TableNumber.Should().Be("5");
        order.Status.Should().Be("Pending");

        _orderServiceMock.Verify(x => x.GetOrderByIdAsync(_testOrderId), Times.Once);
    }

    [Fact]
    public async Task GetOrder_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _orderServiceMock
            .Setup(x => x.GetOrderByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Order with ID {_testOrderId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.GetOrder(_testOrderId));

        _orderServiceMock.Verify(x => x.GetOrderByIdAsync(_testOrderId), Times.Once);
    }

    #endregion

    #region GetOrders Tests

    [Fact]
    public async Task GetOrders_NoFilters_ReturnsAllOrders()
    {
        // Arrange
        var expectedOrders = new List<OrderDto>
        {
            new OrderDto
            {
                Id = Guid.NewGuid(),
                WaiterId = _testUserId,
                WaiterName = "Test Waiter",
                Status = "Pending",
                Type = "DineIn",
                TotalAmount = 25.50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<OrderItemDto>()
            },
            new OrderDto
            {
                Id = Guid.NewGuid(),
                WaiterId = _testUserId,
                WaiterName = "Test Waiter",
                Status = "Completed",
                Type = "Takeaway",
                TotalAmount = 15.00m,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = DateTime.UtcNow,
                Items = new List<OrderItemDto>()
            }
        };

        _orderServiceMock
            .Setup(x => x.GetOrdersAsync(null, null, null, null))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetOrders(null, null, null, null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var orders = okResult!.Value as IEnumerable<OrderDto>;

        orders.Should().NotBeNull();
        orders.Should().HaveCount(2);

        _orderServiceMock.Verify(x => x.GetOrdersAsync(null, null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetOrders_WithFilters_ReturnsFilteredOrders()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;
        var status = OrderStatus.Pending;

        var expectedOrders = new List<OrderDto>
        {
            new OrderDto
            {
                Id = _testOrderId,
                WaiterId = _testUserId,
                WaiterName = "Test Waiter",
                Status = "Pending",
                Type = "DineIn",
                TotalAmount = 25.50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<OrderItemDto>()
            }
        };

        _orderServiceMock
            .Setup(x => x.GetOrdersAsync(_testUserId, fromDate, toDate, status))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetOrders(_testUserId, fromDate, toDate, status);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var orders = okResult!.Value as IEnumerable<OrderDto>;

        orders.Should().NotBeNull();
        orders.Should().HaveCount(1);
        orders!.First().Status.Should().Be("Pending");

        _orderServiceMock.Verify(x => x.GetOrdersAsync(_testUserId, fromDate, toDate, status), Times.Once);
    }

    #endregion

    #region GetActiveOrders Tests

    [Fact]
    public async Task GetActiveOrders_ReturnsOnlyActiveOrders()
    {
        // Arrange
        var expectedOrders = new List<OrderDto>
        {
            new OrderDto
            {
                Id = Guid.NewGuid(),
                Status = "Pending",
                Type = "DineIn",
                TotalAmount = 25.50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<OrderItemDto>()
            },
            new OrderDto
            {
                Id = Guid.NewGuid(),
                Status = "Preparing",
                Type = "DineIn",
                TotalAmount = 30.00m,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                UpdatedAt = DateTime.UtcNow,
                Items = new List<OrderItemDto>()
            }
        };

        _orderServiceMock
            .Setup(x => x.GetActiveOrdersAsync())
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetActiveOrders();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var orders = okResult!.Value as IEnumerable<OrderDto>;

        orders.Should().NotBeNull();
        orders.Should().HaveCount(2);
        orders!.All(o => o.Status != "Completed" && o.Status != "Cancelled").Should().BeTrue();

        _orderServiceMock.Verify(x => x.GetActiveOrdersAsync(), Times.Once);
    }

    #endregion

    #region GetOrdersByTable Tests

    [Fact]
    public async Task GetOrdersByTable_ReturnsTableOrders()
    {
        // Arrange
        var expectedOrders = new List<OrderDto>
        {
            new OrderDto
            {
                Id = Guid.NewGuid(),
                TableId = _testTableId,
                TableNumber = "5",
                Status = "Pending",
                Type = "DineIn",
                TotalAmount = 25.50m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = new List<OrderItemDto>()
            }
        };

        _orderServiceMock
            .Setup(x => x.GetOrdersByTableAsync(_testTableId))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetOrdersByTable(_testTableId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var orders = okResult!.Value as List<OrderDto>;

        orders.Should().NotBeNull();
        orders.Should().HaveCount(1);
        orders![0].TableId.Should().Be(_testTableId);
        orders[0].TableNumber.Should().Be("5");

        _orderServiceMock.Verify(x => x.GetOrdersByTableAsync(_testTableId), Times.Once);
    }

    #endregion

    #region GetOrderItemsByLocation Tests

    [Fact]
    public async Task GetOrderItemsByLocation_Kitchen_ReturnsKitchenItems()
    {
        // Arrange
        var expectedItems = new List<OrderItemDto>
        {
            new OrderItemDto
            {
                Id = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                ProductName = "Pizza Margherita",
                PreparationLocation = "Kitchen",
                Quantity = 2,
                UnitPrice = 12.00m,
                Subtotal = 24.00m,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                SelectedAccompaniments = new List<SelectedAccompanimentDto>()
            }
        };

        _orderServiceMock
            .Setup(x => x.GetOrderItemsByLocationAsync(PreparationLocation.Kitchen, null))
            .ReturnsAsync(expectedItems);

        // Act
        var result = await _controller.GetOrderItemsByLocation("Kitchen", null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var items = okResult!.Value as List<OrderItemDto>;

        items.Should().NotBeNull();
        items.Should().HaveCount(1);
        items![0].PreparationLocation.Should().Be("Kitchen");

        _orderServiceMock.Verify(x => x.GetOrderItemsByLocationAsync(PreparationLocation.Kitchen, null), Times.Once);
    }

    [Fact]
    public async Task GetOrderItemsByLocation_Bar_WithStatus_ReturnsFilteredItems()
    {
        // Arrange
        var expectedItems = new List<OrderItemDto>
        {
            new OrderItemDto
            {
                Id = Guid.NewGuid(),
                ProductName = "Mojito",
                PreparationLocation = "Bar",
                Quantity = 1,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                SelectedAccompaniments = new List<SelectedAccompanimentDto>()
            }
        };

        _orderServiceMock
            .Setup(x => x.GetOrderItemsByLocationAsync(PreparationLocation.Bar, OrderItemStatus.Pending))
            .ReturnsAsync(expectedItems);

        // Act
        var result = await _controller.GetOrderItemsByLocation("Bar", OrderItemStatus.Pending);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var items = okResult!.Value as List<OrderItemDto>;

        items.Should().NotBeNull();
        items.Should().HaveCount(1);
        items![0].PreparationLocation.Should().Be("Bar");
        items[0].Status.Should().Be("Pending");

        _orderServiceMock.Verify(x => x.GetOrderItemsByLocationAsync(PreparationLocation.Bar, OrderItemStatus.Pending), Times.Once);
    }

    #endregion

    #region UpdateOrderStatus Tests

    [Fact]
    public async Task UpdateOrderStatus_ValidStatus_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateOrderStatusDto
        {
            Status = "Preparing"
        };

        _orderServiceMock
            .Setup(x => x.UpdateOrderStatusAsync(_testOrderId, OrderStatus.Preparing))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateOrderStatus(_testOrderId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _orderServiceMock.Verify(x => x.UpdateOrderStatusAsync(_testOrderId, OrderStatus.Preparing), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatus_NonExistingOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateOrderStatusDto { Status = "Preparing" };

        _orderServiceMock
            .Setup(x => x.UpdateOrderStatusAsync(_testOrderId, OrderStatus.Preparing))
            .ThrowsAsync(new KeyNotFoundException($"Order with ID {_testOrderId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.UpdateOrderStatus(_testOrderId, updateDto));

        _orderServiceMock.Verify(x => x.UpdateOrderStatusAsync(_testOrderId, OrderStatus.Preparing), Times.Once);
    }

    #endregion

    #region CompleteOrder Tests

    [Fact]
    public async Task CompleteOrder_ValidOrder_ReturnsNoContent()
    {
        // Arrange
        _orderServiceMock
            .Setup(x => x.CompleteOrderAsync(_testOrderId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CompleteOrder(_testOrderId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _orderServiceMock.Verify(x => x.CompleteOrderAsync(_testOrderId), Times.Once);
    }

    [Fact]
    public async Task CompleteOrder_NonExistingOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        _orderServiceMock
            .Setup(x => x.CompleteOrderAsync(_testOrderId))
            .ThrowsAsync(new KeyNotFoundException($"Order with ID {_testOrderId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.CompleteOrder(_testOrderId));

        _orderServiceMock.Verify(x => x.CompleteOrderAsync(_testOrderId), Times.Once);
    }

    #endregion

    #region CancelOrder Tests

    [Fact]
    public async Task CancelOrder_WithReason_ReturnsNoContent()
    {
        // Arrange
        var cancelDto = new CancelOrderDto
        {
            Reason = "Customer requested cancellation"
        };

        _orderServiceMock
            .Setup(x => x.CancelOrderAsync(_testOrderId, cancelDto.Reason))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CancelOrder(_testOrderId, cancelDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _orderServiceMock.Verify(x => x.CancelOrderAsync(_testOrderId, cancelDto.Reason), Times.Once);
    }

    [Fact]
    public async Task CancelOrder_AlreadyCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var cancelDto = new CancelOrderDto { Reason = "Test" };

        _orderServiceMock
            .Setup(x => x.CancelOrderAsync(_testOrderId, cancelDto.Reason))
            .ThrowsAsync(new InvalidOperationException("Cannot cancel completed order"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.CancelOrder(_testOrderId, cancelDto));

        _orderServiceMock.Verify(x => x.CancelOrderAsync(_testOrderId, cancelDto.Reason), Times.Once);
    }

    #endregion

    #region AddItemToOrder Tests

    [Fact]
    public async Task AddItemToOrder_ValidItem_ReturnsOrderItem()
    {
        // Arrange
        var createItemDto = new CreateOrderItemDto
        {
            ProductId = Guid.NewGuid(),
            Quantity = 2,
            Notes = "Extra spicy",
            SelectedAccompanimentIds = new List<Guid>()
        };

        var expectedItem = new OrderItemDto
        {
            Id = _testOrderItemId,
            ProductId = createItemDto.ProductId,
            ProductName = "Spicy Pizza",
            Quantity = 2,
            UnitPrice = 12.00m,
            Subtotal = 24.00m,
            Notes = "Extra spicy",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            SelectedAccompaniments = new List<SelectedAccompanimentDto>()
        };

        _orderServiceMock
            .Setup(x => x.AddItemToOrderAsync(_testOrderId, It.IsAny<CreateOrderItemDto>()))
            .ReturnsAsync(expectedItem);

        // Act
        var result = await _controller.AddItemToOrder(_testOrderId, createItemDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var item = okResult!.Value as OrderItemDto;

        item.Should().NotBeNull();
        item!.ProductName.Should().Be("Spicy Pizza");
        item.Quantity.Should().Be(2);
        item.Notes.Should().Be("Extra spicy");

        _orderServiceMock.Verify(x => x.AddItemToOrderAsync(_testOrderId, It.IsAny<CreateOrderItemDto>()), Times.Once);
    }

    #endregion

    #region UpdateOrderItemStatus Tests

    [Fact]
    public async Task UpdateOrderItemStatus_ValidStatus_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateOrderItemStatusDto
        {
            Status = "Preparing"
        };

        _orderServiceMock
            .Setup(x => x.UpdateOrderItemStatusAsync(_testOrderItemId, OrderItemStatus.Preparing))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateOrderItemStatus(_testOrderItemId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _orderServiceMock.Verify(x => x.UpdateOrderItemStatusAsync(_testOrderItemId, OrderItemStatus.Preparing), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderItemStatus_NonExistingItem_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateOrderItemStatusDto { Status = "Ready" };

        _orderServiceMock
            .Setup(x => x.UpdateOrderItemStatusAsync(_testOrderItemId, OrderItemStatus.Ready))
            .ThrowsAsync(new KeyNotFoundException($"Order item with ID {_testOrderItemId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.UpdateOrderItemStatus(_testOrderItemId, updateDto));

        _orderServiceMock.Verify(x => x.UpdateOrderItemStatusAsync(_testOrderItemId, OrderItemStatus.Ready), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "Waiter")
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
