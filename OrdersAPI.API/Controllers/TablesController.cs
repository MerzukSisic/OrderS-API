using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Constants;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TablesController(ITableService tableService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TableDto>>> GetTables(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? status = null)
    {
        TableStatus? tableStatus = null;
        if (status != null)
        {
            if (!Enum.TryParse<TableStatus>(status, ignoreCase: true, out var parsed))
                return BadRequest($"Invalid table status '{status}'. Valid values: {string.Join(", ", Enum.GetNames<TableStatus>())}");
            tableStatus = parsed;
        }
        var result = await tableService.GetAllTablesAsync(page, pageSize, tableStatus);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result.Items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TableDto>> GetTable(Guid id)
    {
        var table = await tableService.GetTableByIdAsync(id);
        return Ok(table);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<TableDto>> CreateTable([FromBody] CreateTableDto dto)
    {
        var table = await tableService.CreateTableAsync(dto);
        return CreatedAtAction(nameof(GetTable), new { id = table.Id }, table);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateTable(Guid id, [FromBody] UpdateTableDto dto)
    {
        await tableService.UpdateTableAsync(id, dto);
        return NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateTableStatus(Guid id, [FromQuery] string status)
    {
        if (!Enum.TryParse<TableStatus>(status, ignoreCase: true, out var tableStatus))
            return BadRequest($"Invalid table status '{status}'. Valid values: {string.Join(", ", Enum.GetNames<TableStatus>())}");
        await tableService.UpdateTableStatusAsync(id, tableStatus);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteTable(Guid id)
    {
        await tableService.DeleteTableAsync(id);
        return NoContent();
    }
}