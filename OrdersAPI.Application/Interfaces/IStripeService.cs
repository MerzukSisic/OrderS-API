using OrdersAPI.Application.DTOs;
using Stripe.Checkout;

namespace OrdersAPI.Application.Interfaces;

public interface IStripeService
{
    // Payment Intents
    Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(CreatePaymentIntentDto dto);
    Task<PaymentIntentResponseDto> GetPaymentIntentAsync(string paymentIntentId);
    Task<bool> ConfirmPaymentAsync(string paymentIntentId);
    Task<bool> CancelPaymentIntentAsync(string paymentIntentId);
    
    // Refunds
    Task<RefundResponseDto> RefundPaymentAsync(RefundRequestDto dto);
    Task<RefundResponseDto> GetRefundAsync(string refundId);
    
    // Checkout Sessions (NEW)
    Task<string> CreateCheckoutSessionAsync(string procurementOrderId, decimal amount, string currency);
    Task<Session> GetCheckoutSessionAsync(string sessionId);
    
    // Webhooks
    Task<WebhookEventDto> HandleWebhookAsync(string json, string signature);
}