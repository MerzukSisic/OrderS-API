using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController(
    IStripeService stripeService,
    ApplicationDbContext context,
    ILogger<PaymentsController> logger) : ControllerBase
{
    [HttpPost("create-intent")]
    public async Task<ActionResult<PaymentIntentResponseDto>> CreatePaymentIntent(
        [FromBody] CreatePaymentIntentDto dto)
    {
        var response = await stripeService.CreatePaymentIntentAsync(dto);
        return Ok(response);
    }

    [HttpGet("intent/{paymentIntentId}")]
    public async Task<ActionResult<PaymentIntentResponseDto>> GetPaymentIntent(string paymentIntentId)
    {
        var response = await stripeService.GetPaymentIntentAsync(paymentIntentId);
        return Ok(response);
    }

    [HttpPost("confirm/{paymentIntentId}")]
    public async Task<ActionResult<bool>> ConfirmPayment(string paymentIntentId)
    {
        var isConfirmed = await stripeService.ConfirmPaymentAsync(paymentIntentId);
        return Ok(new { confirmed = isConfirmed });
    }

    [HttpPost("cancel/{paymentIntentId}")]
    public async Task<ActionResult<bool>> CancelPayment(string paymentIntentId)
    {
        var isCancelled = await stripeService.CancelPaymentIntentAsync(paymentIntentId);
        return Ok(new { cancelled = isCancelled });
    }

    [HttpPost("refund")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RefundResponseDto>> RefundPayment([FromBody] RefundRequestDto dto)
    {
        var response = await stripeService.RefundPaymentAsync(dto);
        return Ok(response);
    }

    [HttpGet("refund/{refundId}")]
    public async Task<ActionResult<RefundResponseDto>> GetRefund(string refundId)
    {
        var response = await stripeService.GetRefundAsync(refundId);
        return Ok(response);
    }

    /// <summary>
    /// Stripe webhook endpoint - automatically updates procurement orders when payments complete
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            if (string.IsNullOrEmpty(signature))
            {
                logger.LogWarning("⚠️ Webhook received without Stripe signature");
                return BadRequest("Missing Stripe signature");
            }

            // Parse and validate webhook event
            var eventDto = await stripeService.HandleWebhookAsync(json, signature);

            logger.LogInformation("📨 Webhook received: {EventType} - {EventId}", 
                eventDto.EventType, eventDto.EventId);

            // Handle different event types
            switch (eventDto.EventType)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(eventDto);
                    break;

                case "payment_intent.succeeded":
                    await HandlePaymentSucceeded(eventDto);
                    break;

                case "payment_intent.payment_failed":
                    await HandlePaymentFailed(eventDto);
                    break;

                case "charge.refunded":
                    await HandleChargeRefunded(eventDto);
                    break;

                default:
                    logger.LogInformation("ℹ️ Unhandled webhook event type: {EventType}", eventDto.EventType);
                    break;
            }

            return Ok(new { received = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "❌ Webhook signature verification failed");
            return Unauthorized("Invalid signature");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error processing webhook");
            return StatusCode(500, new { error = "Webhook processing failed" });
        }
    }

    // ==================== WEBHOOK EVENT HANDLERS ====================

    private async Task HandleCheckoutSessionCompleted(WebhookEventDto eventDto)
    {
        logger.LogInformation("✅ Processing checkout.session.completed");

        if (string.IsNullOrEmpty(eventDto.PaymentIntentId))
        {
            logger.LogWarning("⚠️ Checkout session completed but no payment intent ID");
            return;
        }

        // Extract procurement order ID from metadata (passed when creating session)
        // We need to get the full session to access metadata
        // For now, we search by payment intent ID once it's available
        
        // Find procurement order that matches this payment intent
        var procurementOrder = await context.ProcurementOrders
            .Where(o => o.Status == ProcurementStatus.Pending)
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefaultAsync();

        if (procurementOrder != null)
        {
            procurementOrder.Status = ProcurementStatus.Paid;
            procurementOrder.StripePaymentIntentId = eventDto.PaymentIntentId;
            await context.SaveChangesAsync();

            logger.LogInformation("✅ Procurement order {OrderId} marked as PAID via webhook (amount: {Amount})",
                procurementOrder.Id, eventDto.Amount);
            return;
        }

        logger.LogWarning("⚠️ No pending procurement order found for payment intent: {PaymentIntentId}", 
            eventDto.PaymentIntentId);
    }

    private async Task HandlePaymentSucceeded(WebhookEventDto eventDto)
    {
        logger.LogInformation("💰 Payment succeeded for intent: {PaymentIntentId}, amount: {Amount}",
            eventDto.PaymentIntentId, eventDto.Amount);

        var procurementOrder = await context.ProcurementOrders
            .Where(o => o.StripePaymentIntentId == eventDto.PaymentIntentId || 
                       (o.Status == ProcurementStatus.Pending))
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefaultAsync();

        if (procurementOrder != null && procurementOrder.Status == ProcurementStatus.Pending)
        {
            procurementOrder.Status = ProcurementStatus.Paid;
            procurementOrder.StripePaymentIntentId = eventDto.PaymentIntentId;
            await context.SaveChangesAsync();

            logger.LogInformation("✅ Procurement order {OrderId} marked as PAID", procurementOrder.Id);
        }
    }

    private async Task HandlePaymentFailed(WebhookEventDto eventDto)
    {
        logger.LogWarning("❌ Payment FAILED for intent: {PaymentIntentId}", eventDto.PaymentIntentId);
    }

    private async Task HandleChargeRefunded(WebhookEventDto eventDto)
    {
        logger.LogInformation("💸 Refund processed for payment intent: {PaymentIntentId}, amount: {Amount}",
            eventDto.PaymentIntentId, eventDto.Amount);

        var procurementOrder = await context.ProcurementOrders
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == eventDto.PaymentIntentId);

        if (procurementOrder != null)
        {
            logger.LogInformation("💸 Procurement order {OrderId} was refunded", procurementOrder.Id);
        }
    }
}