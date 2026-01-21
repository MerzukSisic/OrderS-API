using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using System.Security.Claims;
using OrdersAPI.Domain.Enums;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class ProcurementControllerTests
{
    private readonly Mock<IProcurementService> _procurementServiceMock;
    private readonly ProcurementController _controller;
    private readonly Guid _testStoreId;
    private readonly Guid _testProcurementOrderId;
    private readonly Guid _testItemId;

    public ProcurementControllerTests()
    {
        _procurementServiceMock = new Mock<IProcurementService>();
        _controller = new ProcurementController(_procurementServiceMock.Object);
        _testStoreId = Guid.NewGuid();
        _testProcurementOrderId = Guid.NewGuid();
        _testItemId = Guid.NewGuid();

        SetupAuthenticatedUser();
    }

    #region GetProcurementOrders Tests

    [Fact]
    public async Task GetProcurementOrders_NoFilter_ReturnsAllOrders()
    {
        // Arrange
        var expectedOrders = new List<ProcurementOrderDto>
        {
            new ProcurementOrderDto
            {
                Id = Guid.NewGuid(),
                StoreId = _testStoreId,
                StoreName = "Main Store",
                Supplier = "Supplier A",
                TotalAmount = 1500.00m,
                Status = "Pending",
                OrderDate = DateTime.UtcNow,
                Items = new List<ProcurementOrderItemDto>()
            },
            new ProcurementOrderDto
            {
                Id = Guid.NewGuid(),
                StoreId = Guid.NewGuid(),
                StoreName = "Branch Store",
                Supplier = "Supplier B",
                TotalAmount = 2000.00m,
                Status = "Received",
                OrderDate = DateTime.UtcNow.AddDays(-5),
                Items = new List<ProcurementOrderItemDto>()
            }
        };

        _procurementServiceMock
            .Setup(x => x.GetAllProcurementOrdersAsync(null))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetProcurementOrders(null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var orders = okResult!.Value as IEnumerable<ProcurementOrderDto>;

        orders.Should().NotBeNull();
        orders.Should().HaveCount(2);
        orders!.First().StoreName.Should().Be("Main Store");
        orders.Last().Supplier.Should().Be("Supplier B");

        _procurementServiceMock.Verify(x => x.GetAllProcurementOrdersAsync(null), Times.Once);
    }

    [Fact]
    public async Task GetProcurementOrders_WithStoreFilter_ReturnsFilteredOrders()
    {
        // Arrange
        var expectedOrders = new List<ProcurementOrderDto>
        {
            new ProcurementOrderDto
            {
                Id = _testProcurementOrderId,
                StoreId = _testStoreId,
                StoreName = "Main Store",
                Supplier = "Supplier A",
                TotalAmount = 1500.00m,
                Status = "Pending",
                OrderDate = DateTime.UtcNow,
                Items = new List<ProcurementOrderItemDto>()
            }
        };

        _procurementServiceMock
            .Setup(x => x.GetAllProcurementOrdersAsync(_testStoreId))
            .ReturnsAsync(expectedOrders);

        // Act
        var result = await _controller.GetProcurementOrders(_testStoreId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var orders = okResult!.Value as IEnumerable<ProcurementOrderDto>;

        orders.Should().NotBeNull();
        orders.Should().HaveCount(1);
        orders!.First().StoreId.Should().Be(_testStoreId);

        _procurementServiceMock.Verify(x => x.GetAllProcurementOrdersAsync(_testStoreId), Times.Once);
    }

    #endregion

    #region GetProcurementOrder Tests

    [Fact]
    public async Task GetProcurementOrder_ExistingId_ReturnsOrder()
    {
        // Arrange
        var expectedOrder = new ProcurementOrderDto
        {
            Id = _testProcurementOrderId,
            StoreId = _testStoreId,
            StoreName = "Main Store",
            Supplier = "Supplier A",
            TotalAmount = 1500.00m,
            Status = "Pending",
            OrderDate = DateTime.UtcNow,
            Items = new List<ProcurementOrderItemDto>
            {
                new ProcurementOrderItemDto
                {
                    Id = _testItemId,
                    StoreProductId = Guid.NewGuid(),
                    StoreProductName = "Flour 10kg",
                    Quantity = 20,
                    UnitCost = 15.00m,
                    Subtotal = 300.00m
                }
            }
        };

        _procurementServiceMock
            .Setup(x => x.GetProcurementOrderByIdAsync(_testProcurementOrderId))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _controller.GetProcurementOrder(_testProcurementOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var order = okResult!.Value as ProcurementOrderDto;

        order.Should().NotBeNull();
        order!.Id.Should().Be(_testProcurementOrderId);
        order.StoreName.Should().Be("Main Store");
        order.TotalAmount.Should().Be(1500.00m);
        order.Items.Should().HaveCount(1);

        _procurementServiceMock.Verify(x => x.GetProcurementOrderByIdAsync(_testProcurementOrderId), Times.Once);
    }

    [Fact]
    public async Task GetProcurementOrder_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _procurementServiceMock
            .Setup(x => x.GetProcurementOrderByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Procurement order with ID {_testProcurementOrderId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.GetProcurementOrder(_testProcurementOrderId));

        _procurementServiceMock.Verify(x => x.GetProcurementOrderByIdAsync(_testProcurementOrderId), Times.Once);
    }

    #endregion

    #region CreateProcurementOrder Tests

    [Fact]
    public async Task CreateProcurementOrder_ValidData_ReturnsCreatedOrder()
    {
        // Arrange
        var createDto = new CreateProcurementDto
        {
            StoreId = _testStoreId,
            Supplier = "Supplier A",
            Notes = "Urgent delivery needed",
            Items = new List<CreateProcurementItemDto>
            {
                new CreateProcurementItemDto
                {
                    StoreProductId = Guid.NewGuid(),
                    Quantity = 20
                },
                new CreateProcurementItemDto
                {
                    StoreProductId = Guid.NewGuid(),
                    Quantity = 50
                }
            }
        };

        var expectedOrder = new ProcurementOrderDto
        {
            Id = _testProcurementOrderId,
            StoreId = _testStoreId,
            StoreName = "Main Store",
            Supplier = "Supplier A",
            TotalAmount = 1500.00m,
            Status = "Pending",
            Notes = "Urgent delivery needed",
            OrderDate = DateTime.UtcNow,
            Items = new List<ProcurementOrderItemDto>()
        };

        _procurementServiceMock
            .Setup(x => x.CreateProcurementOrderAsync(It.IsAny<CreateProcurementDto>()))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _controller.CreateProcurementOrder(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var order = createdResult!.Value as ProcurementOrderDto;

        order.Should().NotBeNull();
        order!.Id.Should().Be(_testProcurementOrderId);
        order.Supplier.Should().Be("Supplier A");
        order.TotalAmount.Should().Be(1500.00m);
        order.Status.Should().Be("Pending");

        createdResult.ActionName.Should().Be(nameof(_controller.GetProcurementOrder));
        createdResult.RouteValues!["id"].Should().Be(_testProcurementOrderId);

        _procurementServiceMock.Verify(x => x.CreateProcurementOrderAsync(It.IsAny<CreateProcurementDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateProcurementOrder_InvalidStoreId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var createDto = new CreateProcurementDto
        {
            StoreId = Guid.NewGuid(),
            Supplier = "Supplier A",
            Items = new List<CreateProcurementItemDto>()
        };

        _procurementServiceMock
            .Setup(x => x.CreateProcurementOrderAsync(It.IsAny<CreateProcurementDto>()))
            .ThrowsAsync(new KeyNotFoundException("Store not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.CreateProcurementOrder(createDto));

        _procurementServiceMock.Verify(x => x.CreateProcurementOrderAsync(It.IsAny<CreateProcurementDto>()), Times.Once);
    }

    #endregion

    #region CreatePaymentIntent Tests

    [Fact]
    public async Task CreatePaymentIntent_ValidOrder_ReturnsClientSecret()
    {
        // Arrange
        var expectedClientSecret = "pi_3AbC123_secret_XYZ";

        //_procurementServiceMock
         //   .Setup(x => x.CreatePaymentIntentAsync(_testProcurementOrderId))
          //  .ReturnsAsync(expectedClientSecret);

        // Act
        var result = await _controller.CreatePaymentIntent(_testProcurementOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var paymentIntent = okResult!.Value as PaymentIntentDto;

        paymentIntent.Should().NotBeNull();
        paymentIntent!.ClientSecret.Should().Be(expectedClientSecret);

        _procurementServiceMock.Verify(x => x.CreatePaymentIntentAsync(_testProcurementOrderId), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentIntent_NonExistingOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        _procurementServiceMock
            .Setup(x => x.CreatePaymentIntentAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException("Procurement order not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.CreatePaymentIntent(_testProcurementOrderId));

        _procurementServiceMock.Verify(x => x.CreatePaymentIntentAsync(_testProcurementOrderId), Times.Once);
    }

    #endregion

    #region ConfirmPayment Tests

    [Fact]
    public async Task ConfirmPayment_ValidPayment_ReturnsNoContent()
    {
        // Arrange
        var confirmDto = new ConfirmPaymentDto
        {
            PaymentIntentId = "pi_3AbC123TestPaymentIntent"
        };

        _procurementServiceMock
            .Setup(x => x.ConfirmPaymentAsync(_testProcurementOrderId, confirmDto.PaymentIntentId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ConfirmPayment(_testProcurementOrderId, confirmDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _procurementServiceMock.Verify(x => x.ConfirmPaymentAsync(_testProcurementOrderId, confirmDto.PaymentIntentId), Times.Once);
    }

    [Fact]
    public async Task ConfirmPayment_InvalidPaymentIntent_ThrowsInvalidOperationException()
    {
        // Arrange
        var confirmDto = new ConfirmPaymentDto
        {
            PaymentIntentId = "invalid_payment_intent"
        };

        _procurementServiceMock
            .Setup(x => x.ConfirmPaymentAsync(_testProcurementOrderId, confirmDto.PaymentIntentId))
            .ThrowsAsync(new InvalidOperationException("Payment intent not found or already processed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.ConfirmPayment(_testProcurementOrderId, confirmDto));

        _procurementServiceMock.Verify(x => x.ConfirmPaymentAsync(_testProcurementOrderId, confirmDto.PaymentIntentId), Times.Once);
    }

    #endregion

    #region UpdateStatus Tests

    [Fact]
    public async Task UpdateStatus_PendingToPaid_ReturnsNoContent()
    {
        // Arrange
        var status = "Paid";

        _procurementServiceMock
            .Setup(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Paid))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateStatus(_testProcurementOrderId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _procurementServiceMock.Verify(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Paid), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_PaidToOrdered_ReturnsNoContent()
    {
        // Arrange
        var status = "Ordered";

        _procurementServiceMock
            .Setup(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Ordered))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateStatus(_testProcurementOrderId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _procurementServiceMock.Verify(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Ordered), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_OrderedToReceived_ReturnsNoContent()
    {
        // Arrange
        var status = "Received";

        _procurementServiceMock
            .Setup(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Received))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateStatus(_testProcurementOrderId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _procurementServiceMock.Verify(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Received), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_CancelOrder_ReturnsNoContent()
    {
        // Arrange
        var status = "Cancelled";

        _procurementServiceMock
            .Setup(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Cancelled))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateStatus(_testProcurementOrderId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _procurementServiceMock.Verify(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Cancelled), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_NonExistingOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        var status = "Paid";

        _procurementServiceMock
            .Setup(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Paid))
            .ThrowsAsync(new KeyNotFoundException("Procurement order not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.UpdateStatus(_testProcurementOrderId, status));

        _procurementServiceMock.Verify(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, ProcurementStatus.Paid), Times.Once);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Paid")]
    [InlineData("Ordered")]
    [InlineData("Received")]
    [InlineData("Cancelled")]
    public async Task UpdateStatus_AllValidStatuses_ReturnsNoContent(string status)
    {
        // Arrange
        var enumStatus = Enum.Parse<ProcurementStatus>(status);

        _procurementServiceMock
            .Setup(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, enumStatus))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateStatus(_testProcurementOrderId, status);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _procurementServiceMock.Verify(x => x.UpdateProcurementStatusAsync(_testProcurementOrderId, enumStatus), Times.Once);
    }

    #endregion

    #region ReceiveProcurement Tests

    [Fact]
    public async Task ReceiveProcurement_ValidData_ReturnsNoContent()
    {
        // Arrange
        var receiveDto = new ReceiveProcurementDto
        {
            Items = new List<ReceiveProcurementItemDto>
            {
                new ReceiveProcurementItemDto
                {
                    ItemId = _testItemId,
                    ReceivedQuantity = 18
                },
                new ReceiveProcurementItemDto
                {
                    ItemId = Guid.NewGuid(),
                    ReceivedQuantity = 50
                }
            },
            Notes = "Received with minor packaging damage"
        };

        _procurementServiceMock
            .Setup(x => x.ReceiveProcurementAsync(_testProcurementOrderId, It.IsAny<ReceiveProcurementDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ReceiveProcurement(_testProcurementOrderId, receiveDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _procurementServiceMock.Verify(x => x.ReceiveProcurementAsync(_testProcurementOrderId, It.IsAny<ReceiveProcurementDto>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveProcurement_PartialReceive_ReturnsNoContent()
    {
        // Arrange
        var receiveDto = new ReceiveProcurementDto
        {
            Items = new List<ReceiveProcurementItemDto>
            {
                new ReceiveProcurementItemDto
                {
                    ItemId = _testItemId,
                    ReceivedQuantity = 15
                }
            },
            Notes = "Partial delivery - remaining items to arrive next week"
        };

        _procurementServiceMock
            .Setup(x => x.ReceiveProcurementAsync(_testProcurementOrderId, It.IsAny<ReceiveProcurementDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ReceiveProcurement(_testProcurementOrderId, receiveDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _procurementServiceMock.Verify(x => x.ReceiveProcurementAsync(_testProcurementOrderId, It.IsAny<ReceiveProcurementDto>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveProcurement_InvalidItemId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var receiveDto = new ReceiveProcurementDto
        {
            Items = new List<ReceiveProcurementItemDto>
            {
                new ReceiveProcurementItemDto
                {
                    ItemId = Guid.NewGuid(),
                    ReceivedQuantity = 20
                }
            }
        };

        _procurementServiceMock
            .Setup(x => x.ReceiveProcurementAsync(_testProcurementOrderId, It.IsAny<ReceiveProcurementDto>()))
            .ThrowsAsync(new KeyNotFoundException("One or more procurement items not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.ReceiveProcurement(_testProcurementOrderId, receiveDto));

        _procurementServiceMock.Verify(x => x.ReceiveProcurementAsync(_testProcurementOrderId, It.IsAny<ReceiveProcurementDto>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveProcurement_AlreadyReceived_ThrowsInvalidOperationException()
    {
        // Arrange
        var receiveDto = new ReceiveProcurementDto
        {
            Items = new List<ReceiveProcurementItemDto>
            {
                new ReceiveProcurementItemDto
                {
                    ItemId = _testItemId,
                    ReceivedQuantity = 20
                }
            }
        };

        _procurementServiceMock
            .Setup(x => x.ReceiveProcurementAsync(_testProcurementOrderId, It.IsAny<ReceiveProcurementDto>()))
            .ThrowsAsync(new InvalidOperationException("Procurement order already received"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.ReceiveProcurement(_testProcurementOrderId, receiveDto));

        _procurementServiceMock.Verify(x => x.ReceiveProcurementAsync(_testProcurementOrderId, It.IsAny<ReceiveProcurementDto>()), Times.Once);
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
