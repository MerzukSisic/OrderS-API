using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ProcurementController(IProcurementService procurementService, ILogger<ProcurementController> logger)
    : ControllerBase
{
    private readonly ILogger<ProcurementController> _logger = logger;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProcurementOrderDto>>> GetProcurementOrders([FromQuery] Guid? storeId = null)
    {
        var orders = await procurementService.GetAllProcurementOrdersAsync(storeId);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProcurementOrderDto>> GetProcurementOrder(Guid id)
    {
        try
        {
            var order = await procurementService.GetProcurementOrderByIdAsync(id);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProcurementOrderDto>> CreateProcurementOrder([FromBody] CreateProcurementDto dto)
    {
        var order = await procurementService.CreateProcurementOrderAsync(dto);
        return CreatedAtAction(nameof(GetProcurementOrder), new { id = order.Id }, order);
    }

    [HttpPost("{id}/payment-intent")]
    public async Task<ActionResult<PaymentIntentDto>> CreatePaymentIntent(Guid id)
    {
        try
        {
            var clientSecret = await procurementService.CreatePaymentIntentAsync(id);
            return Ok(new PaymentIntentDto { ClientSecret = clientSecret });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
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
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] string status)
    {
        try
        {
            var procurementStatus = Enum.Parse<ProcurementStatus>(status);
            await procurementService.UpdateProcurementStatusAsync(id, procurementStatus);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
