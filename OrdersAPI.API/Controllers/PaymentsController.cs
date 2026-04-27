using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Constants;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController(
    IStripeService stripeService,
    IProcurementService procurementService,
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
    [Authorize(Roles = Roles.Admin)]
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
                logger.LogWarning("Webhook received without Stripe signature");
                return BadRequest("Missing Stripe signature");
            }

            var eventDto = await stripeService.HandleWebhookAsync(json, signature);

            logger.LogInformation("Webhook received: {EventType} - {EventId}", eventDto.EventType, eventDto.EventId);

            switch (eventDto.EventType)
            {
                case "checkout.session.completed":
                    await procurementService.HandleWebhookCheckoutCompletedAsync(eventDto);
                    break;

                case "payment_intent.succeeded":
                    await procurementService.HandleWebhookPaymentSucceededAsync(eventDto);
                    break;

                case "payment_intent.payment_failed":
                    logger.LogWarning("Payment FAILED for PI: {PaymentIntentId}", eventDto.PaymentIntentId);
                    break;

                case "charge.refunded":
                    await procurementService.HandleWebhookChargeRefundedAsync(eventDto);
                    break;

                default:
                    logger.LogInformation("Unhandled webhook event type: {EventType}", eventDto.EventType);
                    break;
            }

            return Ok(new { received = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Webhook signature verification failed");
            return Unauthorized("Invalid signature");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing webhook");
            return StatusCode(500, new { error = "Webhook processing failed" });
        }
    }
}
