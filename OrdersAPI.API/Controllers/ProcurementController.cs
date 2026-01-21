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

    // ==================== STRIPE CHECKOUT SESSION ENDPOINTS ====================

    /// <summary>
    /// Creates Stripe Checkout Session for procurement order payment
    /// Returns checkout URL for user to complete payment in browser
    /// </summary>
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

            // Create checkout session
            // NOTE: Payment Intent ID will be null until user actually pays
            // Webhook will receive it when payment completes
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

    /// <summary>
    /// Success redirect URL after Stripe Checkout completion
    /// </summary>
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

            // Verify session with Stripe
            var session = await stripeService.GetCheckoutSessionAsync(session_id);

            if (session.PaymentStatus == "paid")
            {
                // Store payment intent ID and update status
                // (Webhook might have already done this, but we do it here as backup)
                if (order.Status == ProcurementStatus.Pending)
                {
                    order.Status = ProcurementStatus.Paid;
                    order.StripePaymentIntentId = session.PaymentIntentId;
                    await context.SaveChangesAsync();

                    logger.LogInformation("✅ Payment successful for order {OrderId}, PI: {PaymentIntentId}",
                        id, session.PaymentIntentId);
                }
                else
                {
                    logger.LogInformation("ℹ️ Order {OrderId} already in status {Status} (webhook was faster)",
                        id, order.Status);
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

    /// <summary>
    /// Cancel redirect URL if user cancels payment
    /// </summary>
    [HttpGet("{id}/payment-cancel")]
    [AllowAnonymous]
    public IActionResult PaymentCancel(Guid id)
    {
        logger.LogWarning("⚠️ Payment cancelled for order {OrderId}", id);
        return Content(GetCancelHtml(), "text/html");
    }

    // ==================== HTML TEMPLATES ====================

    private static string GetSuccessHtml() => @"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Plaćanje uspješno</title>
            <style>
                body {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    height: 100vh;
                    margin: 0;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                }
                .container {
                    background: white;
                    padding: 3rem;
                    border-radius: 12px;
                    box-shadow: 0 10px 40px rgba(0,0,0,0.2);
                    text-align: center;
                    max-width: 400px;
                }
                .checkmark {
                    width: 80px;
                    height: 80px;
                    border-radius: 50%;
                    display: block;
                    stroke-width: 2;
                    stroke: #4CAF50;
                    stroke-miterlimit: 10;
                    margin: 0 auto 1rem;
                    animation: scale .3s ease-in-out .9s both;
                }
                .checkmark__circle {
                    stroke-dasharray: 166;
                    stroke-dashoffset: 166;
                    stroke-width: 2;
                    stroke: #4CAF50;
                    fill: none;
                    animation: stroke 0.6s cubic-bezier(0.65, 0, 0.45, 1) forwards;
                }
                .checkmark__check {
                    transform-origin: 50% 50%;
                    stroke-dasharray: 48;
                    stroke-dashoffset: 48;
                    animation: stroke 0.3s cubic-bezier(0.65, 0, 0.45, 1) 0.8s forwards;
                }
                @keyframes stroke {
                    100% { stroke-dashoffset: 0; }
                }
                @keyframes scale {
                    0%, 100% { transform: none; }
                    50% { transform: scale3d(1.1, 1.1, 1); }
                }
                h2 { color: #4CAF50; margin-bottom: 1rem; }
                p { color: #666; margin-bottom: 0.5rem; }
                .close-info { color: #999; font-size: 0.9rem; margin-top: 2rem; }
            </style>
        </head>
        <body>
            <div class='container'>
                <svg class='checkmark' xmlns='http://www.w3.org/2000/svg' viewBox='0 0 52 52'>
                    <circle class='checkmark__circle' cx='26' cy='26' r='25' fill='none'/>
                    <path class='checkmark__check' fill='none' d='M14.1 27.2l7.1 7.2 16.7-16.8'/>
                </svg>
                <h2>Plaćanje uspješno!</h2>
                <p>Vaša nabavna narudžba je plaćena.</p>
                <p class='close-info'>Ovaj prozor će se automatski zatvoriti...</p>
            </div>
            <script>
                setTimeout(() => {
                    window.close();
                    window.location.href = 'about:blank';
                }, 3000);
            </script>
        </body>
        </html>";

    private static string GetCancelHtml() => @"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <title>Plaćanje otkazano</title>
            <style>
                body {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    height: 100vh;
                    margin: 0;
                    background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
                }
                .container {
                    background: white;
                    padding: 3rem;
                    border-radius: 12px;
                    box-shadow: 0 10px 40px rgba(0,0,0,0.2);
                    text-align: center;
                    max-width: 400px;
                }
                h2 { color: #f44336; margin-bottom: 1rem; }
                p { color: #666; margin-bottom: 0.5rem; }
                .close-info { color: #999; font-size: 0.9rem; margin-top: 2rem; }
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>Plaćanje otkazano</h2>
                <p>Vaše plaćanje je otkazano.</p>
                <p>Možete pokušati ponovo bilo kada.</p>
                <p class='close-info'>Ovaj prozor će se automatski zatvoriti...</p>
            </div>
            <script>
                setTimeout(() => {
                    window.close();
                    window.location.href = 'about:blank';
                }, 3000);
            </script>
        </body>
        </html>";
}