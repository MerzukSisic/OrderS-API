using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using System.Text;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IStripeService> _stripeServiceMock;
    private readonly PaymentsController _controller;
    private readonly Guid _testOrderId;
    private readonly string _testPaymentIntentId;
    private readonly string _testRefundId;

    public PaymentsControllerTests()
    {
        _stripeServiceMock = new Mock<IStripeService>();
        _controller = new PaymentsController(_stripeServiceMock.Object);
        _testOrderId = Guid.NewGuid();
        _testPaymentIntentId = "pi_3AbC123TestPaymentIntent";
        _testRefundId = "re_1AbC123TestRefund";
    }

    #region CreatePaymentIntent Tests

    [Fact]
    public async Task CreatePaymentIntent_ValidData_ReturnsPaymentIntent()
    {
        // Arrange
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

        // Act
        var result = await _controller.CreatePaymentIntent(createDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as PaymentIntentResponseDto;

        response.Should().NotBeNull();
        response!.PaymentIntentId.Should().Be(_testPaymentIntentId);
        response.ClientSecret.Should().NotBeNullOrEmpty();
        response.Amount.Should().Be(125.50m);
        response.Currency.Should().Be("bam");
        response.Status.Should().Be("requires_payment_method");

        _stripeServiceMock.Verify(x => x.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentDto>()), Times.Once);
    }

    [Fact]
    public async Task CreatePaymentIntent_InvalidAmount_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreatePaymentIntentDto
        {
            OrderId = _testOrderId,
            Amount = -10.00m,
            Currency = "bam"
        };

        _stripeServiceMock
            .Setup(x => x.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentDto>()))
            .ThrowsAsync(new InvalidOperationException("Amount must be positive"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.CreatePaymentIntent(createDto));

        _stripeServiceMock.Verify(x => x.CreatePaymentIntentAsync(It.IsAny<CreatePaymentIntentDto>()), Times.Once);
    }

    #endregion

    #region GetPaymentIntent Tests

    [Fact]
    public async Task GetPaymentIntent_ExistingId_ReturnsPaymentIntent()
    {
        // Arrange
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

        // Act
        var result = await _controller.GetPaymentIntent(_testPaymentIntentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as PaymentIntentResponseDto;

        response.Should().NotBeNull();
        response!.PaymentIntentId.Should().Be(_testPaymentIntentId);
        response.Status.Should().Be("succeeded");
        response.Amount.Should().Be(125.50m);

        _stripeServiceMock.Verify(x => x.GetPaymentIntentAsync(_testPaymentIntentId), Times.Once);
    }

    [Fact]
    public async Task GetPaymentIntent_InvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        _stripeServiceMock
            .Setup(x => x.GetPaymentIntentAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Invalid payment intent ID"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.GetPaymentIntent("invalid_id"));

        _stripeServiceMock.Verify(x => x.GetPaymentIntentAsync("invalid_id"), Times.Once);
    }

    #endregion

    #region ConfirmPayment Tests

    [Fact]
    public async Task ConfirmPayment_SuccessfulPayment_ReturnsTrue()
    {
        // Arrange
        _stripeServiceMock
            .Setup(x => x.ConfirmPaymentAsync(_testPaymentIntentId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ConfirmPayment(_testPaymentIntentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { confirmed = true });

        _stripeServiceMock.Verify(x => x.ConfirmPaymentAsync(_testPaymentIntentId), Times.Once);
    }

    [Fact]
    public async Task ConfirmPayment_FailedPayment_ReturnsFalse()
    {
        // Arrange
        _stripeServiceMock
            .Setup(x => x.ConfirmPaymentAsync(_testPaymentIntentId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ConfirmPayment(_testPaymentIntentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { confirmed = false });

        _stripeServiceMock.Verify(x => x.ConfirmPaymentAsync(_testPaymentIntentId), Times.Once);
    }

    #endregion

    #region CancelPayment Tests

    [Fact]
    public async Task CancelPayment_SuccessfulCancellation_ReturnsTrue()
    {
        // Arrange
        _stripeServiceMock
            .Setup(x => x.CancelPaymentIntentAsync(_testPaymentIntentId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelPayment(_testPaymentIntentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { cancelled = true });

        _stripeServiceMock.Verify(x => x.CancelPaymentIntentAsync(_testPaymentIntentId), Times.Once);
    }

    [Fact]
    public async Task CancelPayment_AlreadyCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        _stripeServiceMock
            .Setup(x => x.CancelPaymentIntentAsync(_testPaymentIntentId))
            .ThrowsAsync(new InvalidOperationException("Cannot cancel completed payment"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.CancelPayment(_testPaymentIntentId));

        _stripeServiceMock.Verify(x => x.CancelPaymentIntentAsync(_testPaymentIntentId), Times.Once);
    }

    #endregion

    #region RefundPayment Tests

    [Fact]
    public async Task RefundPayment_FullRefund_ReturnsRefundResponse()
    {
        // Arrange
        var refundDto = new RefundRequestDto
        {
            PaymentIntentId = _testPaymentIntentId,
            Amount = null, // Full refund
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

        // Act
        var result = await _controller.RefundPayment(refundDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundResponseDto;

        response.Should().NotBeNull();
        response!.RefundId.Should().Be(_testRefundId);
        response.Amount.Should().Be(125.50m);
        response.Status.Should().Be("succeeded");

        _stripeServiceMock.Verify(x => x.RefundPaymentAsync(It.IsAny<RefundRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task RefundPayment_PartialRefund_ReturnsRefundResponse()
    {
        // Arrange
        var refundDto = new RefundRequestDto
        {
            PaymentIntentId = _testPaymentIntentId,
            Amount = 50.00m, // Partial refund
            Reason = "damaged_product"
        };

        var expectedResponse = new RefundResponseDto
        {
            RefundId = _testRefundId,
            Amount = 50.00m,
            Status = "succeeded",
            Reason = "damaged_product"
        };

        _stripeServiceMock
            .Setup(x => x.RefundPaymentAsync(It.IsAny<RefundRequestDto>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefundPayment(refundDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundResponseDto;

        response.Should().NotBeNull();
        response!.Amount.Should().Be(50.00m);

        _stripeServiceMock.Verify(x => x.RefundPaymentAsync(It.IsAny<RefundRequestDto>()), Times.Once);
    }

    [Fact]
    public async Task RefundPayment_InvalidPaymentIntent_ThrowsInvalidOperationException()
    {
        // Arrange
        var refundDto = new RefundRequestDto
        {
            PaymentIntentId = "invalid_id",
            Reason = "test"
        };

        _stripeServiceMock
            .Setup(x => x.RefundPaymentAsync(It.IsAny<RefundRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Payment intent not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.RefundPayment(refundDto));

        _stripeServiceMock.Verify(x => x.RefundPaymentAsync(It.IsAny<RefundRequestDto>()), Times.Once);
    }

    #endregion

    #region GetRefund Tests

    [Fact]
    public async Task GetRefund_ExistingId_ReturnsRefund()
    {
        // Arrange
        var expectedResponse = new RefundResponseDto
        {
            RefundId = _testRefundId,
            Amount = 125.50m,
            Status = "succeeded",
            Reason = "requested_by_customer"
        };

        _stripeServiceMock
            .Setup(x => x.GetRefundAsync(_testRefundId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetRefund(_testRefundId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as RefundResponseDto;

        response.Should().NotBeNull();
        response!.RefundId.Should().Be(_testRefundId);
        response.Amount.Should().Be(125.50m);
        response.Status.Should().Be("succeeded");

        _stripeServiceMock.Verify(x => x.GetRefundAsync(_testRefundId), Times.Once);
    }

    [Fact]
    public async Task GetRefund_InvalidId_ThrowsInvalidOperationException()
    {
        // Arrange
        _stripeServiceMock
            .Setup(x => x.GetRefundAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Refund not found"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.GetRefund("invalid_id"));

        _stripeServiceMock.Verify(x => x.GetRefundAsync("invalid_id"), Times.Once);
    }

    #endregion

    #region HandleWebhook Tests

    [Fact]
    public async Task HandleWebhook_ValidSignature_ReturnsOk()
    {
        // Arrange
        var webhookJson = "{\"type\":\"payment_intent.succeeded\",\"data\":{}}";
        var signature = "t=1234567890,v1=test_signature";

        var expectedEvent = new WebhookEventDto
        {
            EventId = "evt_123",
            EventType = "payment_intent.succeeded",
            PaymentIntentId = _testPaymentIntentId,
            Status = "succeeded",
            Amount = 125.50m
        };

        _stripeServiceMock
            .Setup(x => x.HandleWebhookAsync(webhookJson, signature))
            .ReturnsAsync(expectedEvent);

        // Setup HttpContext
        var httpContext = new DefaultHttpContext();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(webhookJson));
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Stripe-Signature"] = signature;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.HandleWebhook();

        // Assert
        result.Should().BeOfType<OkResult>();

        _stripeServiceMock.Verify(x => x.HandleWebhookAsync(webhookJson, signature), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_InvalidSignature_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var webhookJson = "{\"type\":\"payment_intent.succeeded\"}";
        var invalidSignature = "invalid_signature";

        _stripeServiceMock
            .Setup(x => x.HandleWebhookAsync(webhookJson, invalidSignature))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid webhook signature"));

        // Setup HttpContext
        var httpContext = new DefaultHttpContext();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(webhookJson));
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Stripe-Signature"] = invalidSignature;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _controller.HandleWebhook());

        _stripeServiceMock.Verify(x => x.HandleWebhookAsync(webhookJson, invalidSignature), Times.Once);
    }

    [Fact]
    public async Task HandleWebhook_PaymentFailed_ReturnsOk()
    {
        // Arrange
        var webhookJson = "{\"type\":\"payment_intent.payment_failed\",\"data\":{}}";
        var signature = "t=1234567890,v1=test_signature";

        var expectedEvent = new WebhookEventDto
        {
            EventId = "evt_456",
            EventType = "payment_intent.payment_failed",
            PaymentIntentId = _testPaymentIntentId,
            Status = "failed",
            Amount = 125.50m
        };

        _stripeServiceMock
            .Setup(x => x.HandleWebhookAsync(webhookJson, signature))
            .ReturnsAsync(expectedEvent);

        // Setup HttpContext
        var httpContext = new DefaultHttpContext();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(webhookJson));
        httpContext.Request.Body = stream;
        httpContext.Request.Headers["Stripe-Signature"] = signature;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await _controller.HandleWebhook();

        // Assert
        result.Should().BeOfType<OkResult>();

        _stripeServiceMock.Verify(x => x.HandleWebhookAsync(webhookJson, signature), Times.Once);
    }

    #endregion
}
