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

public class ReceiptsControllerTests
{
    private readonly Mock<IReceiptService> _receiptServiceMock;
    private readonly ReceiptsController _controller;
    private readonly Guid _testOrderId;

    public ReceiptsControllerTests()
    {
        _receiptServiceMock = new Mock<IReceiptService>();
        _controller = new ReceiptsController(_receiptServiceMock.Object);
        _testOrderId = Guid.NewGuid();

        SetupAuthenticatedUser("Waiter");
    }

    #region GetCustomerReceipt Tests

    [Fact]
    public async Task GetCustomerReceipt_ValidOrderId_ReturnsCompleteReceipt()
    {
        // Arrange
        var expectedReceipt = new ReceiptDto
        {
            OrderId = _testOrderId,
            OrderNumber = "ORD-2026-001",
            TableNumber = "5",
            WaiterName = "John Doe",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            CompletedAt = DateTime.UtcNow,
            OrderType = "DineIn",
            Status = "Completed",
            IsPartnerOrder = false,
            Items = new List<ReceiptItemDto>
            {
                new ReceiptItemDto
                {
                    ProductName = "Pizza Margherita",
                    Quantity = 2,
                    UnitPrice = 12.50m,
                    Subtotal = 25.00m,
                    Notes = "Extra cheese",
                    SelectedAccompaniments = new List<string> { "Extra Mozzarella" }
                },
                new ReceiptItemDto
                {
                    ProductName = "Coca Cola",
                    Quantity = 2,
                    UnitPrice = 2.50m,
                    Subtotal = 5.00m,
                    SelectedAccompaniments = new List<string>()
                }
            },
            Subtotal = 30.00m,
            Tax = 5.10m,
            Discount = 0.00m,
            Total = 35.10m,
            Notes = "Customer requested extra napkins"
        };

        _receiptServiceMock
            .Setup(x => x.GenerateCustomerReceiptAsync(_testOrderId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.GetCustomerReceipt(_testOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var receipt = okResult!.Value as ReceiptDto;

        receipt.Should().NotBeNull();
        receipt!.OrderId.Should().Be(_testOrderId);
        receipt.OrderNumber.Should().Be("ORD-2026-001");
        receipt.TableNumber.Should().Be("5");
        receipt.WaiterName.Should().Be("John Doe");
        receipt.Items.Should().HaveCount(2);
        receipt.Subtotal.Should().Be(30.00m);
        receipt.Tax.Should().Be(5.10m);
        receipt.Total.Should().Be(35.10m);

        _receiptServiceMock.Verify(x => x.GenerateCustomerReceiptAsync(_testOrderId), Times.Once);
    }

    [Fact]
    public async Task GetCustomerReceipt_TakeawayOrder_ReturnsReceiptWithoutTable()
    {
        // Arrange
        var expectedReceipt = new ReceiptDto
        {
            OrderId = _testOrderId,
            OrderNumber = "ORD-2026-002",
            TableNumber = null,
            WaiterName = "Jane Smith",
            CreatedAt = DateTime.UtcNow,
            OrderType = "Takeaway",
            Status = "Completed",
            IsPartnerOrder = false,
            Items = new List<ReceiptItemDto>(),
            Subtotal = 15.00m,
            Tax = 2.55m,
            Discount = 0.00m,
            Total = 17.55m
        };

        _receiptServiceMock
            .Setup(x => x.GenerateCustomerReceiptAsync(_testOrderId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.GetCustomerReceipt(_testOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var receipt = okResult!.Value as ReceiptDto;

        receipt.Should().NotBeNull();
        receipt!.TableNumber.Should().BeNull();
        receipt.OrderType.Should().Be("Takeaway");

        _receiptServiceMock.Verify(x => x.GenerateCustomerReceiptAsync(_testOrderId), Times.Once);
    }

    [Fact]
    public async Task GetCustomerReceipt_PartnerOrder_ReturnsReceiptWithPartnerFlag()
    {
        // Arrange
        var expectedReceipt = new ReceiptDto
        {
            OrderId = _testOrderId,
            OrderNumber = "ORD-2026-003",
            TableNumber = "10",
            WaiterName = "Bob Wilson",
            CreatedAt = DateTime.UtcNow,
            OrderType = "DineIn",
            Status = "Completed",
            IsPartnerOrder = true,
            Items = new List<ReceiptItemDto>(),
            Subtotal = 50.00m,
            Tax = 8.50m,
            Discount = 5.00m,
            Total = 53.50m
        };

        _receiptServiceMock
            .Setup(x => x.GenerateCustomerReceiptAsync(_testOrderId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.GetCustomerReceipt(_testOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var receipt = okResult!.Value as ReceiptDto;

        receipt.Should().NotBeNull();
        receipt!.IsPartnerOrder.Should().BeTrue();
        receipt.Discount.Should().Be(5.00m);

        _receiptServiceMock.Verify(x => x.GenerateCustomerReceiptAsync(_testOrderId), Times.Once);
    }

    [Fact]
    public async Task GetCustomerReceipt_NonExistingOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        _receiptServiceMock
            .Setup(x => x.GenerateCustomerReceiptAsync(_testOrderId))
            .ThrowsAsync(new KeyNotFoundException($"Order with ID {_testOrderId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.GetCustomerReceipt(_testOrderId));

        _receiptServiceMock.Verify(x => x.GenerateCustomerReceiptAsync(_testOrderId), Times.Once);
    }

    #endregion

    #region GetKitchenReceipt Tests

    [Fact]
    public async Task GetKitchenReceipt_ValidOrderId_ReturnsKitchenReceipt()
    {
        // Arrange
        SetupAuthenticatedUser("Kitchen");

        var expectedReceipt = new KitchenReceiptDto
        {
            OrderId = _testOrderId,
            OrderNumber = "ORD-2026-004",
            TableNumber = "7",
            WaiterName = "Alice Cooper",
            CreatedAt = DateTime.UtcNow,
            OrderType = "DineIn",
            Items = new List<KitchenReceiptItemDto>
            {
                new KitchenReceiptItemDto
                {
                    ProductName = "Pizza Margherita",
                    Quantity = 2,
                    Notes = "Extra cheese, no olives",
                    SelectedAccompaniments = new List<string> { "Extra Mozzarella" },
                    Ingredients = new List<string> 
                    { 
                        "Tomato Sauce 100g",
                        "Mozzarella 150g",
                        "Basil leaves"
                    }
                },
                new KitchenReceiptItemDto
                {
                    ProductName = "Chicken Wings",
                    Quantity = 1,
                    Notes = "Spicy",
                    SelectedAccompaniments = new List<string> { "BBQ Sauce" },
                    Ingredients = new List<string> 
                    { 
                        "Chicken Wings 500g",
                        "BBQ Spice Mix 20g"
                    }
                }
            }
        };

        _receiptServiceMock
            .Setup(x => x.GenerateKitchenReceiptAsync(_testOrderId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.GetKitchenReceipt(_testOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var receipt = okResult!.Value as KitchenReceiptDto;

        receipt.Should().NotBeNull();
        receipt!.OrderId.Should().Be(_testOrderId);
        receipt.OrderNumber.Should().Be("ORD-2026-004");
        receipt.Items.Should().HaveCount(2);
        receipt.Items[0].ProductName.Should().Be("Pizza Margherita");
        receipt.Items[0].Ingredients.Should().HaveCount(3);
        receipt.Items[0].SelectedAccompaniments.Should().Contain("Extra Mozzarella");

        _receiptServiceMock.Verify(x => x.GenerateKitchenReceiptAsync(_testOrderId), Times.Once);
    }

    [Fact]
    public async Task GetKitchenReceipt_OnlyKitchenItems_ReturnsFilteredReceipt()
    {
        // Arrange
        SetupAuthenticatedUser("Kitchen");

        var expectedReceipt = new KitchenReceiptDto
        {
            OrderId = _testOrderId,
            OrderNumber = "ORD-2026-005",
            TableNumber = "3",
            WaiterName = "Test Waiter",
            CreatedAt = DateTime.UtcNow,
            OrderType = "DineIn",
            Items = new List<KitchenReceiptItemDto>
            {
                new KitchenReceiptItemDto
                {
                    ProductName = "Pasta Carbonara",
                    Quantity = 1,
                    Notes = null,
                    SelectedAccompaniments = new List<string>(),
                    Ingredients = new List<string> { "Pasta 200g", "Eggs 2pcs", "Bacon 100g" }
                }
            }
        };

        _receiptServiceMock
            .Setup(x => x.GenerateKitchenReceiptAsync(_testOrderId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.GetKitchenReceipt(_testOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var receipt = okResult!.Value as KitchenReceiptDto;

        receipt.Should().NotBeNull();
        receipt!.Items.Should().HaveCount(1);
        receipt.Items[0].Ingredients.Should().NotBeEmpty();

        _receiptServiceMock.Verify(x => x.GenerateKitchenReceiptAsync(_testOrderId), Times.Once);
    }

    [Fact]
    public async Task GetKitchenReceipt_NonExistingOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        SetupAuthenticatedUser("Kitchen");

        _receiptServiceMock
            .Setup(x => x.GenerateKitchenReceiptAsync(_testOrderId))
            .ThrowsAsync(new KeyNotFoundException($"Order with ID {_testOrderId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.GetKitchenReceipt(_testOrderId));

        _receiptServiceMock.Verify(x => x.GenerateKitchenReceiptAsync(_testOrderId), Times.Once);
    }

    #endregion

    #region GetBarReceipt Tests

    [Fact]
    public async Task GetBarReceipt_ValidOrderId_ReturnsBarReceipt()
    {
        // Arrange
        SetupAuthenticatedUser("Bartender");

        var expectedReceipt = new BarReceiptDto
        {
            OrderId = _testOrderId,
            OrderNumber = "ORD-2026-006",
            TableNumber = "12",
            WaiterName = "Mike Johnson",
            CreatedAt = DateTime.UtcNow,
            OrderType = "DineIn",
            Items = new List<BarReceiptItemDto>
            {
                new BarReceiptItemDto
                {
                    ProductName = "Mojito",
                    Quantity = 2,
                    Notes = "Extra mint",
                    SelectedAccompaniments = new List<string> { "Fresh Mint" }
                },
                new BarReceiptItemDto
                {
                    ProductName = "Coca Cola",
                    Quantity = 3,
                    Notes = "With ice",
                    SelectedAccompaniments = new List<string>()
                }
            }
        };

        _receiptServiceMock
            .Setup(x => x.GenerateBarReceiptAsync(_testOrderId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.GetBarReceipt(_testOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var receipt = okResult!.Value as BarReceiptDto;

        receipt.Should().NotBeNull();
        receipt!.OrderId.Should().Be(_testOrderId);
        receipt.OrderNumber.Should().Be("ORD-2026-006");
        receipt.Items.Should().HaveCount(2);
        receipt.Items[0].ProductName.Should().Be("Mojito");
        receipt.Items[0].Quantity.Should().Be(2);
        receipt.Items[0].Notes.Should().Be("Extra mint");

        _receiptServiceMock.Verify(x => x.GenerateBarReceiptAsync(_testOrderId), Times.Once);
    }

    [Fact]
    public async Task GetBarReceipt_OnlyBarItems_ReturnsFilteredReceipt()
    {
        // Arrange
        SetupAuthenticatedUser("Bartender");

        var expectedReceipt = new BarReceiptDto
        {
            OrderId = _testOrderId,
            OrderNumber = "ORD-2026-007",
            TableNumber = "8",
            WaiterName = "Sarah Lee",
            CreatedAt = DateTime.UtcNow,
            OrderType = "DineIn",
            Items = new List<BarReceiptItemDto>
            {
                new BarReceiptItemDto
                {
                    ProductName = "Beer Heineken",
                    Quantity = 4,
                    Notes = null,
                    SelectedAccompaniments = new List<string>()
                }
            }
        };

        _receiptServiceMock
            .Setup(x => x.GenerateBarReceiptAsync(_testOrderId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _controller.GetBarReceipt(_testOrderId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var receipt = okResult!.Value as BarReceiptDto;

        receipt.Should().NotBeNull();
        receipt!.Items.Should().HaveCount(1);
        receipt.Items[0].ProductName.Should().Be("Beer Heineken");

        _receiptServiceMock.Verify(x => x.GenerateBarReceiptAsync(_testOrderId), Times.Once);
    }

    [Fact]
    public async Task GetBarReceipt_NonExistingOrder_ThrowsKeyNotFoundException()
    {
        // Arrange
        SetupAuthenticatedUser("Bartender");

        _receiptServiceMock
            .Setup(x => x.GenerateBarReceiptAsync(_testOrderId))
            .ThrowsAsync(new KeyNotFoundException($"Order with ID {_testOrderId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.GetBarReceipt(_testOrderId));

        _receiptServiceMock.Verify(x => x.GenerateBarReceiptAsync(_testOrderId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, $"test@example.com"),
            new Claim(ClaimTypes.Role, role)
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
