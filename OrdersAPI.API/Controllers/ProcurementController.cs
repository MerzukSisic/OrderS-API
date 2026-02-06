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
[Authorize(Roles = "Admin")]
public class ProcurementController(
    IProcurementService procurementService,
    IStripeService stripeService,
    ApplicationDbContext context,
    ILogger<ProcurementController> logger)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProcurementOrderDto>>> GetProcurementOrders([FromQuery] Guid? storeId = null)
    {
        var orders = await procurementService.GetAllProcurementOrdersAsync(storeId);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProcurementOrderDto>> GetProcurementOrder(Guid id)
    {
        var order = await procurementService.GetProcurementOrderByIdAsync(id);
        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<ProcurementOrderDto>> CreateProcurementOrder([FromBody] CreateProcurementDto dto)
    {
        var order = await procurementService.CreateProcurementOrderAsync(dto);
        return CreatedAtAction(nameof(GetProcurementOrder), new { id = order.Id }, order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
    {
        var procurementStatus = Enum.Parse<ProcurementStatus>(status);
        await procurementService.UpdateProcurementStatusAsync(id, procurementStatus);
        return NoContent();
    }

    [HttpPost("{id}/receive")]
    public async Task<IActionResult> ReceiveProcurement(Guid id, [FromBody] ReceiveProcurementDto dto)
    {
        await procurementService.ReceiveProcurementAsync(id, dto);
        return NoContent();
    }

    // ==================== MOBILE: PAYMENT INTENT (IN-APP) ====================
    
    [HttpPost("{id}/payment-intent")]
    public async Task<ActionResult<PaymentIntentDto>> CreatePaymentIntent(Guid id)
    {
        try
        {
            // DEBUG: log mode secret key-a (TEST vs LIVE)
            logger.LogWarning("🔑 Stripe SecretKey mode: {Mode}",
                Stripe.StripeConfiguration.ApiKey?.StartsWith("sk_test_") == true ? "TEST" : "LIVE/INVALID");

            var paymentIntent = await procurementService.CreatePaymentIntentAsync(id);

            return Ok(new PaymentIntentDto
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.PaymentIntentId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating payment intent for order {OrderId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("{id}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(Guid id, [FromBody] ConfirmPaymentDto dto)
    {
        try
        {
            await procurementService.ConfirmPaymentAsync(id, dto.PaymentIntentId);
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error confirming payment for order {OrderId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // ==================== DESKTOP: CHECKOUT SESSION (BROWSER) ====================

    [HttpPost("{id}/create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession(Guid id)
    {
        try
        {
            var order = await context.ProcurementOrders.FindAsync(id);
            if (order == null)
                return NotFound(new { error = "Order not found" });

            if (order.Status != ProcurementStatus.Pending)
                return BadRequest(new { error = "Order is not in pending status" });

            var checkoutUrl = await stripeService.CreateCheckoutSessionAsync(
                id.ToString(),
                order.TotalAmount,
                "bam"
            );

            logger.LogInformation("✅ Checkout session created for order {OrderId}", id);

            return Ok(new { checkoutUrl });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating checkout session for order {OrderId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}/payment-success")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentSuccess(Guid id, [FromQuery] string session_id)
    {
        try
        {
            var order = await context.ProcurementOrders.FindAsync(id);
            if (order == null)
            {
                logger.LogWarning("⚠️ Order {OrderId} not found for session {SessionId}", id, session_id);
                return NotFound();
            }

            var session = await stripeService.GetCheckoutSessionAsync(session_id);

            if (session.PaymentStatus == "paid")
            {
                if (order.Status == ProcurementStatus.Pending)
                {
                    order.Status = ProcurementStatus.Paid;
                    order.StripePaymentIntentId = session.PaymentIntentId;
                    await context.SaveChangesAsync();

                    logger.LogInformation("✅ Payment successful for order {OrderId}, PI: {PaymentIntentId}",
                        id, session.PaymentIntentId);
                }

                return Content(GetSuccessHtml(), "text/html");
            }

            logger.LogWarning("⚠️ Payment not completed for order {OrderId}, status: {PaymentStatus}",
                id, session.PaymentStatus);
            
            return BadRequest(new { error = "Payment not completed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing payment success for order {OrderId}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{id}/payment-cancel")]
    [AllowAnonymous]
    public IActionResult PaymentCancel(Guid id)
    {
        logger.LogWarning("⚠️ Payment cancelled for order {OrderId}", id);
        return Content(GetCancelHtml(), "text/html");
    }

    private static string GetSuccessHtml() => @"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Plaćanje uspješno</title>
            <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
                .container { background: white; padding: 3rem; border-radius: 12px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); text-align: center; max-width: 400px; }
                h2 { color: #4CAF50; margin-bottom: 1rem; }
                p { color: #666; margin-bottom: 0.5rem; }
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>Plaćanje uspješno!</h2>
                <p>Vaša nabavna narudžba je plaćena.</p>
            </div>
            <script>setTimeout(() => window.close(), 3000);</script>
        </body>
        </html>";

    private static string GetCancelHtml() => @"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Plaćanje otkazano</title>
            <style>
                body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); }
                .container { background: white; padding: 3rem; border-radius: 12px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); text-align: center; max-width: 400px; }
                h2 { color: #f44336; margin-bottom: 1rem; }
                p { color: #666; margin-bottom: 0.5rem; }
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>Plaćanje otkazano</h2>
                <p>Možete pokušati ponovo bilo kada.</p>
            </div>
            <script>setTimeout(() => window.close(), 3000);</script>
        </body>
        </html>";
}