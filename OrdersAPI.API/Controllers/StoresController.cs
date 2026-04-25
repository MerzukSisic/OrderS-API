using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class StoresController(IStoreService storeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StoreDto>>> GetStores([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var result = await storeService.GetAllStoresAsync(page, pageSize);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result.Items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StoreDto>> GetStore(Guid id)
    {
        var store = await storeService.GetStoreByIdAsync(id);
        return Ok(store);
    }

    [HttpPost]
    public async Task<ActionResult<StoreDto>> CreateStore([FromBody] CreateStoreDto dto)
    {
        var store = await storeService.CreateStoreAsync(dto);
        return CreatedAtAction(nameof(GetStore), new { id = store.Id }, store);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStore(Guid id, [FromBody] UpdateStoreDto dto)
    {
        await storeService.UpdateStoreAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStore(Guid id)
    {
        await storeService.DeleteStoreAsync(id);
        return NoContent();
    }
}