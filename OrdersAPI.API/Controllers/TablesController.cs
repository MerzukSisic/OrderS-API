using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TablesController(ITableService tableService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TableDto>>> GetTables()
    {
        var tables = await tableService.GetAllTablesAsync();
        return Ok(tables);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TableDto>> GetTable(Guid id)
    {
        var table = await tableService.GetTableByIdAsync(id);
        return Ok(table);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TableDto>> CreateTable([FromBody] CreateTableDto dto)
    {
        var table = await tableService.CreateTableAsync(dto);
        return CreatedAtAction(nameof(GetTable), new { id = table.Id }, table);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTable(Guid id, [FromBody] UpdateTableDto dto)
    {
        await tableService.UpdateTableAsync(id, dto);
        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateTableStatus(Guid id, [FromQuery] string status)
    {
        var tableStatus = Enum.Parse<TableStatus>(status);
        await tableService.UpdateTableStatusAsync(id, tableStatus);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTable(Guid id)
    {
        await tableService.DeleteTableAsync(id);
        return NoContent();
    }
}