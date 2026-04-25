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
        logger.LogInformation("✅ Processing checkout.session.completed for PI: {PaymentIntentId}", eventDto.PaymentIntentId);

        // 1. Resolve by explicit procurement order ID from session metadata (most reliable)
        if (!string.IsNullOrEmpty(eventDto.ProcurementOrderId) &&
            Guid.TryParse(eventDto.ProcurementOrderId, out var metadataOrderId))
        {
            var order = await context.ProcurementOrders.FindAsync(metadataOrderId);
            if (order != null)
            {
                if (order.Status == ProcurementStatus.Paid)
                {
                    logger.LogInformation("ℹ️ Order {OrderId} already paid, skipping (idempotent)", order.Id);
                    return;
                }
                if (order.Status != ProcurementStatus.Pending)
                {
                    logger.LogWarning("⚠️ Order {OrderId} is in status {Status}, cannot transition to Paid", order.Id, order.Status);
                    return;
                }
                order.Status = ProcurementStatus.Paid;
                order.StripePaymentIntentId = eventDto.PaymentIntentId;
                await context.SaveChangesAsync();
                logger.LogInformation("✅ Procurement order {OrderId} marked as PAID via checkout metadata", order.Id);
                return;
            }
        }

        // 2. Fallback: lookup by PaymentIntentId (already stored from confirm-payment call)
        if (!string.IsNullOrEmpty(eventDto.PaymentIntentId))
        {
            var order = await context.ProcurementOrders
                .FirstOrDefaultAsync(o => o.StripePaymentIntentId == eventDto.PaymentIntentId);
            if (order != null)
            {
                if (order.Status == ProcurementStatus.Paid) return;
                if (order.Status != ProcurementStatus.Pending)
                {
                    logger.LogWarning("⚠️ Order {OrderId} is in status {Status}, cannot transition to Paid (PI fallback)", order.Id, order.Status);
                    return;
                }
                order.Status = ProcurementStatus.Paid;
                await context.SaveChangesAsync();
                logger.LogInformation("✅ Procurement order {OrderId} marked as PAID via PI fallback", order.Id);
                return;
            }
        }

        logger.LogWarning("⚠️ No matching procurement order found for checkout webhook PI: {PaymentIntentId}", eventDto.PaymentIntentId);
    }

    private async Task HandlePaymentSucceeded(WebhookEventDto eventDto)
    {
        logger.LogInformation("💰 payment_intent.succeeded: PI={PaymentIntentId} Amount={Amount}",
            eventDto.PaymentIntentId, eventDto.Amount);

        // 1. Exact match by PaymentIntentId already stored on the order
        var procurementOrder = await context.ProcurementOrders
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == eventDto.PaymentIntentId);

        // 2. Metadata-based lookup (payment intent carries procurementOrderId in its metadata)
        if (procurementOrder == null &&
            !string.IsNullOrEmpty(eventDto.ProcurementOrderId) &&
            Guid.TryParse(eventDto.ProcurementOrderId, out var procId))
        {
            procurementOrder = await context.ProcurementOrders.FindAsync(procId);
        }

        if (procurementOrder == null)
        {
            logger.LogInformation("ℹ️ payment_intent.succeeded: no matching procurement order – may be a regular order payment");
            return;
        }

        // Idempotency: skip if already handled
        if (procurementOrder.Status != ProcurementStatus.Pending)
        {
            logger.LogInformation("ℹ️ Order {OrderId} already in status {Status}, skipping", procurementOrder.Id, procurementOrder.Status);
            return;
        }

        procurementOrder.Status = ProcurementStatus.Paid;
        if (string.IsNullOrEmpty(procurementOrder.StripePaymentIntentId))
            procurementOrder.StripePaymentIntentId = eventDto.PaymentIntentId;
        await context.SaveChangesAsync();

        logger.LogInformation("✅ Procurement order {OrderId} marked as PAID via payment_intent.succeeded", procurementOrder.Id);
    }

    private Task HandlePaymentFailed(WebhookEventDto eventDto)
    {
        logger.LogWarning("❌ Payment FAILED for PI: {PaymentIntentId}", eventDto.PaymentIntentId);
        return Task.CompletedTask;
    }

    private async Task HandleChargeRefunded(WebhookEventDto eventDto)
    {
        logger.LogInformation("💸 charge.refunded for PI: {PaymentIntentId}, amount: {Amount}",
            eventDto.PaymentIntentId, eventDto.Amount);

        var procurementOrder = await context.ProcurementOrders
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == eventDto.PaymentIntentId);

        if (procurementOrder != null)
        {
            logger.LogInformation("💸 Procurement order {OrderId} refunded", procurementOrder.Id);
        }
    }
}