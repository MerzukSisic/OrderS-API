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

public class ProcurementControllerTests : TestBase
{
    private readonly Mock<IProcurementService> _procurementServiceMock;
    private readonly Mock<ILogger<ProcurementController>> _loggerMock;
    private readonly ProcurementController _controller;

    public ProcurementControllerTests()
    {
        _procurementServiceMock = new Mock<IProcurementService>();
        _loggerMock = new Mock<ILogger<ProcurementController>>();
        _controller = new ProcurementController(_procurementServiceMock.Object, _loggerMock.Object);
        _controller.ControllerContext = CreateControllerContext(Guid.NewGuid());
    }

    [Fact]
    public async Task GetProcurementOrders_ReturnsOkWithOrders()
    {
        // Arrange
        var orders = new List<ProcurementOrderDto>
        {
            new() { Id = Guid.NewGuid(), Supplier = "Supplier A", TotalAmount = 500m },
            new() { Id = Guid.NewGuid(), Supplier = "Supplier B", TotalAmount = 750m }
        };

        _procurementServiceMock.Setup(x => x.GetAllProcurementOrdersAsync(null))
            .ReturnsAsync(orders);

        // Act
        var result = await _controller.GetProcurementOrders();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedOrders = okResult.Value.Should().BeAssignableTo<IEnumerable<ProcurementOrderDto>>().Subject;
        returnedOrders.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateProcurementOrder_ValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateProcurementDto
        {
            StoreId = Guid.NewGuid(),
            Supplier = "Test Supplier",
            Items = new List<CreateProcurementItemDto>
            {
                new() { StoreProductId = Guid.NewGuid(), Quantity = 100 }
            }
        };

        var createdOrder = new ProcurementOrderDto
        {
            Id = Guid.NewGuid(),
            Supplier = "Test Supplier",
            TotalAmount = 1000m
        };

        _procurementServiceMock.Setup(x => x.CreateProcurementOrderAsync(createDto))
            .ReturnsAsync(createdOrder);

        // Act
        var result = await _controller.CreateProcurementOrder(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedOrder = createdResult.Value.Should().BeOfType<ProcurementOrderDto>().Subject;
        returnedOrder.Supplier.Should().Be("Test Supplier");
    }

    [Fact]
    public async Task CreatePaymentIntent_ValidOrder_ReturnsOkWithClientSecret()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var clientSecret = "pi_test_secret";

        _procurementServiceMock.Setup(x => x.CreatePaymentIntentAsync(orderId))
            .ReturnsAsync(clientSecret);

        // Act
        var result = await _controller.CreatePaymentIntent(orderId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<PaymentIntentDto>().Subject;
        response.ClientSecret.Should().Be(clientSecret);
    }

    [Fact]
    public async Task UpdateStatus_ValidStatus_ReturnsNoContent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var status = "Received";

        _procurementServiceMock.Setup(x => x.UpdateProcurementStatusAsync(orderId, ProcurementStatus.Received))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateStatus(orderId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
