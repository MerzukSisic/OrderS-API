using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TablesController : ControllerBase
{
    private readonly ITableService _tableService;
    private readonly ILogger<TablesController> _logger;

    public TablesController(ITableService tableService, ILogger<TablesController> logger)
    {
        _tableService = tableService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TableDto>>> GetTables()
    {
        var tables = await _tableService.GetAllTablesAsync();
        return Ok(tables);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TableDto>> GetTable(Guid id)
    {
        try
        {
            var table = await _tableService.GetTableByIdAsync(id);
            return Ok(table);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TableDto>> CreateTable([FromBody] CreateTableDto dto)
    {
        var table = await _tableService.CreateTableAsync(dto);
        return CreatedAtAction(nameof(GetTable), new { id = table.Id }, table);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTable(Guid id, [FromBody] UpdateTableDto dto)
    {
        try
        {
            await _tableService.UpdateTableAsync(id, dto);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateTableStatus(Guid id, [FromQuery] string status)
    {
        try
        {
            var tableStatus = Enum.Parse<TableStatus>(status);
            await _tableService.UpdateTableStatusAsync(id, tableStatus);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTable(Guid id)
    {
        try
        {
            await _tableService.DeleteTableAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
