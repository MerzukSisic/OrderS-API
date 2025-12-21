using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceiptsController : ControllerBase
{
    private readonly IReceiptService _receiptService;
    private readonly ILogger<ReceiptsController> _logger;

    public ReceiptsController(IReceiptService receiptService, ILogger<ReceiptsController> logger)
    {
        _receiptService = receiptService;
        _logger = logger;
    }

    [HttpGet("customer/{orderId}")]
    public async Task<ActionResult<ReceiptDto>> GetCustomerReceipt(Guid orderId)
    {
        try
        {
            var receipt = await _receiptService.GenerateCustomerReceiptAsync(orderId);
            return Ok(receipt);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("kitchen/{orderId}")]
    public async Task<ActionResult<KitchenReceiptDto>> GetKitchenReceipt(Guid orderId)
    {
        try
        {
            var receipt = await _receiptService.GenerateKitchenReceiptAsync(orderId);
            return Ok(receipt);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("bar/{orderId}")]
    public async Task<ActionResult<BarReceiptDto>> GetBarReceipt(Guid orderId)
    {
        try
        {
            var receipt = await _receiptService.GenerateBarReceiptAsync(orderId);
            return Ok(receipt);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
