using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Tests.Helpers;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class OrdersControllerTests : TestBase
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly Mock<ILogger<OrdersController>> _loggerMock;
    private readonly OrdersController _controller;
    private readonly Guid _testUserId;

    public OrdersControllerTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _loggerMock = new Mock<ILogger<OrdersController>>();
        _testUserId = Guid.NewGuid();
        _controller = new OrdersController(_orderServiceMock.Object, _loggerMock.Object);
        _controller.ControllerContext = CreateControllerContext(_testUserId, "Waiter");
    }

    [Fact]
    public async Task CreateOrder_ValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateOrderDto
        {
            TableId = Guid.NewGuid(),
            Type = "DineIn",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 2 }
            }
        };

        var createdOrder = new OrderDto
        {
            Id = Guid.NewGuid(),
            WaiterId = _testUserId,
            TotalAmount = 25.00m,
            Status = "Pending"
        };

        _orderServiceMock.Setup(x => x.CreateOrderAsync(_testUserId, createDto))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await _controller.CreateOrder(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedOrder = createdResult.Value.Should().BeOfType<OrderDto>().Subject;
        returnedOrder.TotalAmount.Should().Be(25.00m);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateOrderDto
        {
            TableId = Guid.NewGuid(),
            Type = "DineIn",
            Items = new List<CreateOrderItemDto>
            {
                new() { ProductId = Guid.NewGuid(), Quantity = 100 }
            }
        };

        _orderServiceMock.Setup(x => x.CreateOrderAsync(_testUserId, createDto))
            .ThrowsAsync(new InvalidOperationException("Insufficient stock"));

        // Act
        var result = await _controller.CreateOrder(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetOrder_ExistingId_ReturnsOkWithOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new OrderDto { Id = orderId, Status = "Pending" };

        _orderServiceMock.Setup(x => x.GetOrderByIdAsync(orderId))
            .ReturnsAsync(order);

        // Act
        var result = await _controller.GetOrder(orderId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOrder = okResult.Value.Should().BeOfType<OrderDto>().Subject;
        returnedOrder.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetOrders_ReturnsOkWithOrders()
    {
        // Arrange
        var orders = new List<OrderDto>
        {
            new() { Id = Guid.NewGuid(), Status = "Pending" },
            new() { Id = Guid.NewGuid(), Status = "Completed" }
        };

        _orderServiceMock.Setup(x => x.GetOrdersAsync(null, null, null, null))
            .ReturnsAsync(orders);

        // Act
        var result = await _controller.GetOrders();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOrders = okResult.Value.Should().BeAssignableTo<IEnumerable<OrderDto>>().Subject;
        returnedOrders.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveOrders_ReturnsOnlyActiveOrders()
    {
        // Arrange
        var orders = new List<OrderDto>
        {
            new() { Id = Guid.NewGuid(), Status = "Pending" },
            new() { Id = Guid.NewGuid(), Status = "Preparing" }
        };

        _orderServiceMock.Setup(x => x.GetActiveOrdersAsync())
            .ReturnsAsync(orders);

        // Act
        var result = await _controller.GetActiveOrders();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOrders = okResult.Value.Should().BeAssignableTo<IEnumerable<OrderDto>>().Subject;
        returnedOrders.Should().HaveCount(2);
        returnedOrders.Should().AllSatisfy(o => o.Status.Should().NotBe("Completed"));
    }

    [Fact]
    public async Task UpdateOrderStatus_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var statusDto = new UpdateOrderStatusDto { Status = "Completed" };

        _orderServiceMock.Setup(x => x.UpdateOrderStatusAsync(orderId, OrderStatus.Completed))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateOrderStatus(orderId, statusDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
