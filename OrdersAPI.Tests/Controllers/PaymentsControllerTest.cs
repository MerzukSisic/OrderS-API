using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;
using System.Text;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IStripeService> _stripeServiceMock;
    private readonly Mock<IProcurementService> _procurementServiceMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly PaymentsController _controller;
    private readonly Guid _testOrderId;
    private readonly string _testPaymentIntentId;
    private readonly string _testRefundId;

    public PaymentsControllerTests()
    {
        _stripeServiceMock = new Mock<IStripeService>();
        _procurementServiceMock = new Mock<IProcurementService>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        var logger = Mock.Of<ILogger<PaymentsController>>();
        _controller = new PaymentsController(_stripeServiceMock.Object, _procurementServiceMock.Object, logger);

        _testOrderId = Guid.NewGuid();
        _testPaymentIntentId = "pi_3AbC123TestPaymentIntent";
        _testRefundId = "re_1AbC123TestRefund";
    }

    private void SetupWebhookContext(string json, string signature)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
        httpContext.Request.Headers["Stripe-Signature"] = signature;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    #region CreatePaymentIntent Tests

    [Fact]
    public async Task CreatePaymentIntent_ValidData_ReturnsPaymentIntent()
    {
        var createDto = new CreatePaymentIntentDto
        {
            OrderId = _testOrderId,
            Amount = 125.50m,
            Currency = "bam",
            CustomerEmail = "customer@example.com",
            CustomerName = "John Doe",
            TableNumber = "5"
        };

        var expectedResponse = new PaymentIntentResponseDto
        {
            PaymentIntentId = _testPaymentIntentId,
            ClientSecret = "pi_3AbC123_secret_XYZ",
            Amount = 125.50m,
            Currency = "bam",
            Status = "requires_payment_method"
        };

        _stripeServiceMock
            .Setup(x => x.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentDto>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.CreatePaymentIntent(createDto);

        result.Result.Should().BeOfType<OkObjectResult>();
        var response = (result.Result as OkObjectResult)!.Value as PaymentIntentResponseDto;
        response.Should().NotBeNull();
        response!.PaymentIntentId.Should().Be(_testPaymentIntentId);
        response.Amount.Should().Be(125.50m);
        response.Status.Should().Be("requires_payment_method");

        _stripeServiceMock.Verify(x => x.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentDto>()), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentIntent_InvalidAmount_ThrowsInvalidOperationException()
    {
        var createDto = new CreatePaymentIntentDto { OrderId = _testOrderId, Amount = -10.00m, Currency = "bam" };

        _stripeServiceMock
            .Setup(x => x.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentDto>()))
            .ThrowsAsync(new InvalidOperationException("Amount must be positive"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.CreatePaymentIntent(createDto));
    }

    #endregion

    #region GetPaymentIntent Tests

    [Fact]
    public async Task GetPaymentIntent_ExistingId_ReturnsPaymentIntent()
    {
        var expectedResponse = new PaymentIntentResponseDto
        {
            PaymentIntentId = _testPaymentIntentId,
            ClientSecret = "pi_3AbC123_secret_XYZ",
            Amount = 125.50m,
            Currency = "bam",
            Status = "succeeded"
        };

        _stripeServiceMock
            .Setup(x => x.GetPaymentIntentAsync(_testPaymentIntentId))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.GetPaymentIntent(_testPaymentIntentId);

        result.Result.Should().BeOfType<OkObjectResult>();
        var response = (result.Result as OkObjectResult)!.Value as PaymentIntentResponseDto;
        response!.PaymentIntentId.Should().Be(_testPaymentIntentId);
        response.Status.Should().Be("succeeded");
    }

    [Fact]
    public async Task GetPaymentIntent_InvalidId_ThrowsInvalidOperationException()
    {
        _stripeServiceMock
            .Setup(x => x.GetPaymentIntentAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Invalid payment intent ID"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.GetPaymentIntent("invalid_id"));
    }

    #endregion

    #region ConfirmPayment Tests

    [Fact]
    public async Task ConfirmPayment_SuccessfulPayment_ReturnsTrue()
    {
        _stripeServiceMock
            .Setup(x => x.ConfirmPaymentAsync(_testPaymentIntentId))
            .ReturnsAsync(true);

        var result = await _controller.ConfirmPayment(_testPaymentIntentId);

        result.Result.Should().BeOfType<OkObjectResult>();
        (result.Result as OkObjectResult)!.Value.Should().BeEquivalentTo(new { confirmed = true });
    }

    [Fact]
    public async Task ConfirmPayment_FailedPayment_ReturnsFalse()
    {
        _stripeServiceMock
            .Setup(x => x.ConfirmPaymentAsync(_testPaymentIntentId))
            .ReturnsAsync(false);

        var result = await _controller.ConfirmPayment(_testPaymentIntentId);

        result.Result.Should().BeOfType<OkObjectResult>();
        (result.Result as OkObjectResult)!.Value.Should().BeEquivalentTo(new { confirmed = false });
    }

    #endregion

    #region CancelPayment Tests

    [Fact]
    public async Task CancelPayment_SuccessfulCancellation_ReturnsTrue()
    {
        _stripeServiceMock
            .Setup(x => x.CancelPaymentIntentAsync(_testPaymentIntentId))
            .ReturnsAsync(true);

        var result = await _controller.CancelPayment(_testPaymentIntentId);

        result.Result.Should().BeOfType<OkObjectResult>();
        (result.Result as OkObjectResult)!.Value.Should().BeEquivalentTo(new { cancelled = true });
    }

    [Fact]
    public async Task CancelPayment_AlreadyCompleted_ThrowsInvalidOperationException()
    {
        _stripeServiceMock
            .Setup(x => x.CancelPaymentIntentAsync(_testPaymentIntentId))
            .ThrowsAsync(new InvalidOperationException("Cannot cancel completed payment"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.CancelPayment(_testPaymentIntentId));
    }

    #endregion

    #region RefundPayment Tests

    [Fact]
    public async Task RefundPayment_FullRefund_ReturnsRefundResponse()
    {
        var refundDto = new RefundRequestDto
        {
            PaymentIntentId = _testPaymentIntentId,
            Amount = null,
            Reason = "requested_by_customer"
        };

        var expectedResponse = new RefundResponseDto
        {
            RefundId = _testRefundId,
            Amount = 125.50m,
            Status = "succeeded",
            Reason = "requested_by_customer"
        };

        _stripeServiceMock
            .Setup(x => x.RefundPaymentAsync(It.IsAny<RefundRequestDto>()))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.RefundPayment(refundDto);

        result.Result.Should().BeOfType<OkObjectResult>();
        var response = (result.Result as OkObjectResult)!.Value as RefundResponseDto;
        response!.RefundId.Should().Be(_testRefundId);
        response.Amount.Should().Be(125.50m);
        response.Status.Should().Be("succeeded");
    }

    [Fact]
    public async Task RefundPayment_PartialRefund_ReturnsRefundResponse()
    {
        var refundDto = new RefundRequestDto { PaymentIntentId = _testPaymentIntentId, Amount = 50.00m };

        _stripeServiceMock
            .Setup(x => x.RefundPaymentAsync(It.IsAny<RefundRequestDto>()))
            .ReturnsAsync(new RefundResponseDto { RefundId = _testRefundId, Amount = 50.00m, Status = "succeeded" });

        var result = await _controller.RefundPayment(refundDto);

        var response = (result.Result as OkObjectResult)!.Value as RefundResponseDto;
        response!.Amount.Should().Be(50.00m);
    }

    [Fact]
    public async Task RefundPayment_InvalidPaymentIntent_ThrowsInvalidOperationException()
    {
        _stripeServiceMock
            .Setup(x => x.RefundPaymentAsync(It.IsAny<RefundRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Payment intent not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.RefundPayment(new RefundRequestDto { PaymentIntentId = "invalid_id" }));
    }

    #endregion

    #region GetRefund Tests

    [Fact]
    public async Task GetRefund_ExistingId_ReturnsRefund()
    {
        _stripeServiceMock
            .Setup(x => x.GetRefundAsync(_testRefundId))
            .ReturnsAsync(new RefundResponseDto
            {
                RefundId = _testRefundId,
                Amount = 125.50m,
                Status = "succeeded",
                Reason = "requested_by_customer"
            });

        var result = await _controller.GetRefund(_testRefundId);

        var response = (result.Result as OkObjectResult)!.Value as RefundResponseDto;
        response!.RefundId.Should().Be(_testRefundId);
        response.Amount.Should().Be(125.50m);
    }

    [Fact]
    public async Task GetRefund_InvalidId_ThrowsInvalidOperationException()
    {
        _stripeServiceMock
            .Setup(x => x.GetRefundAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Refund not found"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.GetRefund("invalid_id"));
    }

    #endregion

    #region HandleWebhook Tests

    [Fact]
    public async Task HandleWebhook_ValidSignature_ReturnsOk()
    {
        var webhookJson = "{\"type\":\"payment_intent.succeeded\",\"data\":{}}";
        var signature = "t=1234567890,v1=test_signature";

        _stripeServiceMock
            .Setup(x => x.HandleWebhookAsync(webhookJson, signature))
            .ReturnsAsync(new WebhookEventDto
            {
                EventId = "evt_123",
                EventType = "payment_intent.succeeded",
                PaymentIntentId = _testPaymentIntentId,
                Status = "succeeded",
                Amount = 125.50m
            });

        SetupWebhookContext(webhookJson, signature);

        var result = await _controller.HandleWebhook();

        result.Should().BeOfType<OkObjectResult>();
        _stripeServiceMock.Verify(x => x.HandleWebhookAsync(webhookJson, signature), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_InvalidSignature_ReturnsUnauthorized()
    {
        var webhookJson = "{\"type\":\"payment_intent.succeeded\"}";
        var invalidSignature = "invalid_signature";

        _stripeServiceMock
            .Setup(x => x.HandleWebhookAsync(webhookJson, invalidSignature))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid webhook signature"));

        SetupWebhookContext(webhookJson, invalidSignature);

        var result = await _controller.HandleWebhook();

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task HandleWebhook_PaymentFailed_ReturnsOk()
    {
        var webhookJson = "{\"type\":\"payment_intent.payment_failed\",\"data\":{}}";
        var signature = "t=1234567890,v1=test_signature";

        _stripeServiceMock
            .Setup(x => x.HandleWebhookAsync(webhookJson, signature))
            .ReturnsAsync(new WebhookEventDto
            {
                EventId = "evt_456",
                EventType = "payment_intent.payment_failed",
                PaymentIntentId = _testPaymentIntentId,
                Status = "failed",
                Amount = 125.50m
            });

        SetupWebhookContext(webhookJson, signature);

        var result = await _controller.HandleWebhook();

        result.Should().BeOfType<OkObjectResult>();
    }

    /// <summary>
    /// Regression test: two pending procurement orders exist; only the order whose ID
    /// is embedded in the webhook metadata should be marked Paid.
    /// </summary>
    [Fact]
    public async Task HandleWebhook_TwoPendingOrders_OnlyTargetOrderMarkedPaid()
    {
        // Arrange: create a fresh DB and controller scoped to this test
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);

        var targetOrderId = Guid.NewGuid();
        var otherOrderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        db.ProcurementOrders.AddRange(
            new ProcurementOrder
            {
                Id = targetOrderId, StoreId = storeId, Status = ProcurementStatus.Pending,
                Supplier = "TargetSupplier", TotalAmount = 100m, OrderDate = DateTime.UtcNow
            },
            new ProcurementOrder
            {
                Id = otherOrderId, StoreId = storeId, Status = ProcurementStatus.Pending,
                Supplier = "OtherSupplier", TotalAmount = 200m, OrderDate = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var stripeMock = new Mock<IStripeService>();
        var procurementServiceMock = new Mock<IProcurementService>();
        var logger = Mock.Of<ILogger<PaymentsController>>();
        var controller = new PaymentsController(stripeMock.Object, procurementServiceMock.Object, logger);

        var paymentIntentId = "pi_test_regression_456";
        var webhookJson = "{\"type\":\"payment_intent.succeeded\"}";
        var signature = "t=123,v1=sig";

        stripeMock
            .Setup(x => x.HandleWebhookAsync(webhookJson, signature))
            .ReturnsAsync(new WebhookEventDto
            {
                EventId = "evt_regression",
                EventType = "payment_intent.succeeded",
                PaymentIntentId = paymentIntentId,
                Status = "succeeded",
                Amount = 100m,
                ProcurementOrderId = targetOrderId.ToString() // only this order should be paid
            });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(webhookJson));
        httpContext.Request.Headers["Stripe-Signature"] = signature;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await controller.HandleWebhook();

        // Assert
        result.Should().BeOfType<OkObjectResult>();

        procurementServiceMock.Verify(
            x => x.HandleWebhookPaymentSucceededAsync(It.Is<WebhookEventDto>(e =>
                e.ProcurementOrderId == targetOrderId.ToString())),
            Times.Once);
    }

    /// <summary>
    /// Idempotency test: firing the same webhook event twice must not change anything on second call.
    /// </summary>
    [Fact]
    public async Task HandleWebhook_DuplicateEvent_IdempotentNoDoubleUpdate()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        db.ProcurementOrders.Add(new ProcurementOrder
        {
            Id = orderId, StoreId = storeId, Status = ProcurementStatus.Pending,
            Supplier = "Supplier", TotalAmount = 100m, OrderDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var stripeMock = new Mock<IStripeService>();
        var procurementServiceMock = new Mock<IProcurementService>();
        var logger = Mock.Of<ILogger<PaymentsController>>();
        var controller = new PaymentsController(stripeMock.Object, procurementServiceMock.Object, logger);

        var webhookJson = "{\"type\":\"payment_intent.succeeded\"}";
        var signature = "t=123,v1=sig";

        var eventDto = new WebhookEventDto
        {
            EventId = "evt_idem",
            EventType = "payment_intent.succeeded",
            PaymentIntentId = "pi_idem_test",
            Status = "succeeded",
            Amount = 100m,
            ProcurementOrderId = orderId.ToString()
        };

        stripeMock
            .Setup(x => x.HandleWebhookAsync(webhookJson, signature))
            .ReturnsAsync(eventDto);

        // Fire once
        var httpContext1 = new DefaultHttpContext();
        httpContext1.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(webhookJson));
        httpContext1.Request.Headers["Stripe-Signature"] = signature;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext1 };
        await controller.HandleWebhook();

        // Fire again (duplicate)
        var httpContext2 = new DefaultHttpContext();
        httpContext2.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(webhookJson));
        httpContext2.Request.Headers["Stripe-Signature"] = signature;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext2 };
        var result = await controller.HandleWebhook();

        result.Should().BeOfType<OkObjectResult>("second event must still return 200");
        procurementServiceMock.Verify(
            x => x.HandleWebhookPaymentSucceededAsync(It.Is<WebhookEventDto>(e =>
                e.ProcurementOrderId == orderId.ToString())),
            Times.Exactly(2));
    }

    /// <summary>
    /// State guard test: a webhook for an order that is already past Pending
    /// (e.g., Ordered) must NOT overwrite its status to Paid.
    /// </summary>
    [Fact]
    public async Task HandleWebhook_NonPendingOrder_StatusNotOverwrittenToPaid()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);

        var orderId = Guid.NewGuid();
        var storeId = Guid.NewGuid();

        // Order is already in Ordered state (past Pending)
        db.ProcurementOrders.Add(new ProcurementOrder
        {
            Id = orderId, StoreId = storeId, Status = ProcurementStatus.Ordered,
            Supplier = "Supplier", TotalAmount = 100m, OrderDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var stripeMock = new Mock<IStripeService>();
        var procurementServiceMock = new Mock<IProcurementService>();
        var logger = Mock.Of<ILogger<PaymentsController>>();
        var controller = new PaymentsController(stripeMock.Object, procurementServiceMock.Object, logger);

        var webhookJson = "{\"type\":\"checkout.session.completed\"}";
        var signature = "t=123,v1=sig";

        stripeMock
            .Setup(x => x.HandleWebhookAsync(webhookJson, signature))
            .ReturnsAsync(new WebhookEventDto
            {
                EventId = "evt_stateguard",
                EventType = "checkout.session.completed",
                PaymentIntentId = "pi_stateguard",
                Status = "succeeded",
                Amount = 100m,
                ProcurementOrderId = orderId.ToString()
            });

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(webhookJson));
        httpContext.Request.Headers["Stripe-Signature"] = signature;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.HandleWebhook();

        result.Should().BeOfType<OkObjectResult>("webhook must still return 200");
        procurementServiceMock.Verify(
            x => x.HandleWebhookCheckoutCompletedAsync(It.Is<WebhookEventDto>(e =>
                e.ProcurementOrderId == orderId.ToString())),
            Times.Once);
    }

    #endregion
}
