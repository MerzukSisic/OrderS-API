using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    [HttpGet("store-products")]
    public async Task<ActionResult<IEnumerable<StoreProductDto>>> GetStoreProducts([FromQuery] Guid? storeId = null)
    {
        var products = await inventoryService.GetAllStoreProductsAsync(storeId);
        return Ok(products);
    }

    [HttpGet("store-products/{id}")]
    public async Task<ActionResult<StoreProductDto>> GetStoreProduct(Guid id)
    {
        var product = await inventoryService.GetStoreProductByIdAsync(id);
        return Ok(product);
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
        await inventoryService.UpdateStoreProductAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("store-products/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteStoreProduct(Guid id)
    {
        await inventoryService.DeleteStoreProductAsync(id);
        return NoContent();
    }

    [HttpPost("store-products/{id}/adjust")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdjustInventory(Guid id, [FromBody] AdjustInventoryDto dto)
    {
        await inventoryService.AdjustInventoryAsync(id, dto);
        return NoContent();
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<StoreProductDto>>> GetLowStockProducts()
    {
        var products = await inventoryService.GetLowStockProductsAsync();
        return Ok(products);
    }

    [HttpGet("logs")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<InventoryLogDto>>> GetInventoryLogs(
        [FromQuery] Guid? storeProductId = null,
        [FromQuery] int days = 30)
    {
        var logs = await inventoryService.GetInventoryLogsAsync(storeProductId, days);
        return Ok(logs);
    }

    [HttpGet("total-value")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> GetTotalStockValue([FromQuery] Guid? storeId = null)
    {
        var totalValue = await inventoryService.GetTotalStockValueAsync(storeId);
        return Ok(new { totalValue, currency = "BAM" });
    }

    [HttpGet("consumption-forecast")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ConsumptionForecastDto>>> GetConsumptionForecast([FromQuery] int days = 30)
    {
        var forecast = await inventoryService.GetConsumptionForecastAsync(days);
        return Ok(forecast);
    }
}
