using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReceiptsController(IReceiptService receiptService) : ControllerBase
{
    [HttpGet("customer/{orderId}")]
    public async Task<ActionResult<ReceiptDto>> GetCustomerReceipt(Guid orderId)
    {
        var receipt = await receiptService.GenerateCustomerReceiptAsync(orderId);
        return Ok(receipt);
    }

    [HttpGet("kitchen/{orderId}")]
    public async Task<ActionResult<KitchenReceiptDto>> GetKitchenReceipt(Guid orderId)
    {
        var receipt = await receiptService.GenerateKitchenReceiptAsync(orderId);
        return Ok(receipt);
    }

    [HttpGet("bar/{orderId}")] 
    public async Task<ActionResult<BarReceiptDto>> GetBarReceipt(Guid orderId)
    {
        var receipt = await receiptService.GenerateBarReceiptAsync(orderId);
        return Ok(receipt);
    }
}