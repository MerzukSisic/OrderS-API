using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Constants;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatusOptionsController(IStatusOptionService statusOptionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StatusOptionDto>>> GetAll([FromQuery] string? category = null)
    {
        var options = await statusOptionService.GetAllAsync(category);
        return Ok(options);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StatusOptionDto>> GetById(int id)
    {
        var option = await statusOptionService.GetByIdAsync(id);
        return Ok(option);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<StatusOptionDto>> Create([FromBody] CreateStatusOptionDto dto)
    {
        var created = await statusOptionService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateStatusOptionDto dto)
    {
        await statusOptionService.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await statusOptionService.DeleteAsync(id);
        return NoContent();
    }
}
