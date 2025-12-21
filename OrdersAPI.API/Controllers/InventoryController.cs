using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
    : ControllerBase
{
    private readonly ILogger<InventoryController> _logger = logger;

    [HttpGet("store-products")]
    public async Task<ActionResult<IEnumerable<StoreProductDto>>> GetStoreProducts([FromQuery] Guid? storeId = null)
    {
        var products = await inventoryService.GetAllStoreProductsAsync(storeId);
        return Ok(products);
    }

    [HttpGet("store-products/{id}")]
    public async Task<ActionResult<StoreProductDto>> GetStoreProduct(Guid id)
    {
        try
        {
            var product = await inventoryService.GetStoreProductByIdAsync(id);
            return Ok(product);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("store-products")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StoreProductDto>> CreateStoreProduct([FromBody] CreateStoreProductDto dto)
    {
        var product = await inventoryService.CreateStoreProductAsync(dto);
        return CreatedAtAction(nameof(GetStoreProduct), new { id = product.Id }, product);
    }

    [HttpPut("store-products/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStoreProduct(Guid id, [FromBody] UpdateStoreProductDto dto)
    {
        try
        {
            await inventoryService.UpdateStoreProductAsync(id, dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("store-products/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteStoreProduct(Guid id)
    {
        try
        {
            await inventoryService.DeleteStoreProductAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("store-products/{id}/adjust")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdjustInventory(Guid id, [FromBody] AdjustInventoryDto dto)
    {
        try
        {
            await inventoryService.AdjustInventoryAsync(id, dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<StoreProductDto>>> GetLowStockProducts()
    {
        var products = await inventoryService.GetLowStockProductsAsync();
        return Ok(products);
    }

    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<InventoryLogDto>>> GetInventoryLogs(
        [FromQuery] Guid? storeProductId = null,
        [FromQuery] int days = 30)
    {
        var logs = await inventoryService.GetInventoryLogsAsync(storeProductId, days);
        return Ok(logs);
    }
}
