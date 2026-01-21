using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace OrdersAPI.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeService> _logger;
    private readonly string _webhookSecret;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        _webhookSecret = _configuration["Stripe:WebhookSecret"] ?? string.Empty;
    
        // ✅ DODAJ OVO ZA DEBUG
        _logger.LogWarning("🔑 Webhook Secret loaded: {Secret}", 
            string.IsNullOrEmpty(_webhookSecret) ? "EMPTY!" : $"{_webhookSecret.Substring(0, 15)}...");
    }
    public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(CreatePaymentIntentDto dto)
    {
        try
        {
            var (amount, currency) = ConvertCurrency(dto.Amount, dto.Currency);

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency,
                PaymentMethodTypes = new List<string> { "card" },
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", dto.OrderId.ToString() },
                    { "tableNumber", dto.TableNumber ?? "N/A" },
                    { "originalAmount", dto.Amount.ToString("F2") },
                    { "originalCurrency", dto.Currency.ToUpper() }
                },
                Description = $"Order #{dto.OrderId.ToString().Substring(0, 8)} - Table {dto.TableNumber}",
                ReceiptEmail = dto.CustomerEmail
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            _logger.LogInformation("Payment intent created: {PaymentIntentId} for order {OrderId}, amount {Amount} {Currency}",
                paymentIntent.Id, dto.OrderId, dto.Amount, dto.Currency.ToUpper());

            return new PaymentIntentResponseDto
            {
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Amount = dto.Amount,
                Currency = dto.Currency,
                Status = paymentIntent.Status
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payment intent for order {OrderId}", dto.OrderId);
            throw new InvalidOperationException($"Payment processing error: {ex.StripeError.Message}", ex);
        }
    }

    public async Task<PaymentIntentResponseDto> GetPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            var originalAmount = paymentIntent.Metadata.TryGetValue("originalAmount", out var amtStr) 
                ? decimal.Parse(amtStr) 
                : paymentIntent.Amount / 100m;

            var originalCurrency = paymentIntent.Metadata.TryGetValue("originalCurrency", out var curr) 
                ? curr.ToLower() 
                : paymentIntent.Currency;

            return new PaymentIntentResponseDto
            {
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Amount = originalAmount,
                Currency = originalCurrency,
                Status = paymentIntent.Status
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error retrieving payment intent {PaymentIntentId}", paymentIntentId);
            throw new InvalidOperationException($"Error retrieving payment: {ex.StripeError.Message}", ex);
        }
    }

    public async Task<bool> ConfirmPaymentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            var isSuccessful = paymentIntent.Status == "succeeded";

            _logger.LogInformation("Payment intent {PaymentIntentId} status: {Status}", 
                paymentIntentId, paymentIntent.Status);

            return isSuccessful;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error confirming payment {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }

    public async Task<bool> CancelPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.CancelAsync(paymentIntentId);

            _logger.LogInformation("Payment intent {PaymentIntentId} cancelled", paymentIntentId);

            return paymentIntent.Status == "canceled";
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error cancelling payment intent {PaymentIntentId}", paymentIntentId);
            throw new InvalidOperationException($"Error cancelling payment: {ex.StripeError.Message}", ex);
        }
    }

    public async Task<RefundResponseDto> RefundPaymentAsync(RefundRequestDto dto)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = dto.PaymentIntentId,
                Reason = dto.Reason
            };

            if (dto.Amount.HasValue)
            {
                options.Amount = (long)(dto.Amount.Value * 100);
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(options);

            _logger.LogInformation("Refund created: {RefundId} for payment intent {PaymentIntentId}, amount {Amount}",
                refund.Id, dto.PaymentIntentId, refund.Amount / 100m);

            return new RefundResponseDto
            {
                RefundId = refund.Id,
                Amount = refund.Amount / 100m,
                Status = refund.Status,
                Reason = refund.Reason ?? "N/A"
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error creating refund for payment intent {PaymentIntentId}", dto.PaymentIntentId);
            throw new InvalidOperationException($"Refund error: {ex.StripeError.Message}", ex);
        }
    }

    public async Task<RefundResponseDto> GetRefundAsync(string refundId)
    {
        try
        {
            var service = new RefundService();
            var refund = await service.GetAsync(refundId);

            return new RefundResponseDto
            {
                RefundId = refund.Id,
                Amount = refund.Amount / 100m,
                Status = refund.Status,
                Reason = refund.Reason ?? "N/A"
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error retrieving refund {RefundId}", refundId);
            throw new InvalidOperationException($"Error retrieving refund: {ex.StripeError.Message}", ex);
        }
    }

    // ========== STRIPE CHECKOUT SESSION ==========

    public async Task<string> CreateCheckoutSessionAsync(
        string procurementOrderId, 
        decimal amount, 
        string currency)
    {
        var (checkoutUrl, _) = await CreateCheckoutSessionWithIntentAsync(procurementOrderId, amount, currency);
        return checkoutUrl;
    }

    public async Task<(string checkoutUrl, string paymentIntentId)> CreateCheckoutSessionWithIntentAsync(
        string procurementOrderId,
        decimal amount,
        string currency)
    {
        try
        {
            var (convertedAmount, convertedCurrency) = ConvertCurrency(amount, currency);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = convertedCurrency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Procurement Order",
                                Description = $"Order ID: {procurementOrderId}",
                            },
                            UnitAmount = (long)(convertedAmount * 100),
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = $"http://localhost:5220/api/procurement/{procurementOrderId}/payment-success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"http://localhost:5220/api/procurement/{procurementOrderId}/payment-cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "procurementOrderId", procurementOrderId },
                    { "orderType", "procurement" },
                    { "originalAmount", amount.ToString("F2") },
                    { "originalCurrency", currency.ToUpper() }
                },
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        { "procurementOrderId", procurementOrderId },
                        { "orderType", "procurement" }
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("✅ Checkout session created: {SessionId} for order {OrderId}, PI: {PaymentIntentId}",
                session.Id, procurementOrderId, session.PaymentIntentId);

            return (session.Url, session.PaymentIntentId ?? string.Empty);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "❌ Error creating checkout session for order {OrderId}", procurementOrderId);
            throw new InvalidOperationException($"Checkout session error: {ex.StripeError.Message}", ex);
        }
    }

    public async Task<Session> GetCheckoutSessionAsync(string sessionId)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);
            
            _logger.LogInformation("Retrieved checkout session: {SessionId}, status: {PaymentStatus}", 
                sessionId, session.PaymentStatus);
            
            return session;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Error retrieving checkout session {SessionId}", sessionId);
            throw new InvalidOperationException($"Error retrieving session: {ex.StripeError.Message}", ex);
        }
    }

    // ========== WEBHOOKS ==========

    public async Task<WebhookEventDto> HandleWebhookAsync(string json, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(json, signature, _webhookSecret, throwOnApiVersionMismatch: false );

            _logger.LogInformation("Webhook received: {EventType} - {EventId}", 
                stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        return new WebhookEventDto
                        {
                            EventId = stripeEvent.Id,
                            EventType = stripeEvent.Type,
                            PaymentIntentId = paymentIntent.Id,
                            Status = paymentIntent.Status,
                            Amount = paymentIntent.Amount / 100m
                        };
                    }
                    break;
                }

                case "payment_intent.payment_failed":
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    if (paymentIntent != null)
                    {
                        return new WebhookEventDto
                        {
                            EventId = stripeEvent.Id,
                            EventType = stripeEvent.Type,
                            PaymentIntentId = paymentIntent.Id,
                            Status = paymentIntent.Status,
                            Amount = paymentIntent.Amount / 100m
                        };
                    }
                    break;
                }

                case "checkout.session.completed":
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null)
                    {
                        return new WebhookEventDto
                        {
                            EventId = stripeEvent.Id,
                            EventType = stripeEvent.Type,
                            PaymentIntentId = session.PaymentIntentId ?? string.Empty,
                            Status = session.PaymentStatus,
                            Amount = session.AmountTotal.HasValue ? session.AmountTotal.Value / 100m : 0
                        };
                    }
                    break;
                }

                case "charge.refunded":
                {
                    var charge = stripeEvent.Data.Object as Charge;
                    if (charge != null)
                    {
                        return new WebhookEventDto
                        {
                            EventId = stripeEvent.Id,
                            EventType = stripeEvent.Type,
                            PaymentIntentId = charge.PaymentIntentId ?? string.Empty,
                            Status = "refunded",
                            Amount = charge.AmountRefunded / 100m
                        };
                    }
                    break;
                }
            }

            return new WebhookEventDto
            {
                EventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                PaymentIntentId = string.Empty,
                Status = "unhandled",
                Amount = 0
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Webhook signature verification failed");
            throw new UnauthorizedAccessException("Invalid webhook signature", ex);
        }
    }

    // ========== HELPER METHODS ==========

    private static (decimal amount, string currency) ConvertCurrency(decimal amount, string currency)
    {
        if (currency.ToUpper() is "BAM" or "KM")
        {
            const decimal BAM_TO_EUR = 0.51129m;
            var eurAmount = Math.Round(amount * BAM_TO_EUR, 2);
            return (eurAmount, "eur");
        }

        return (amount, currency.ToLower());
    }
}