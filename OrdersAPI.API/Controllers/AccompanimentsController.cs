using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccompanimentsController(IAccompanimentService accompanimentService) : ControllerBase
{
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<List<AccompanimentGroupDto>>> GetByProduct(Guid productId)
    {
        var groups = await accompanimentService.GetByProductIdAsync(productId);
        return Ok(groups);
    }

    [HttpPost("groups")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<AccompanimentGroupDto>> CreateGroup(CreateAccompanimentGroupDto dto)
    {
        var group = await accompanimentService.CreateGroupAsync(dto);
        return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
    }

    [HttpGet("groups/{id}")]
    public async Task<ActionResult<AccompanimentGroupDto>> GetGroup(Guid id)
    {
        var group = await accompanimentService.GetGroupByIdAsync(id);
        if (group == null)
            return NotFound(new { message = $"Accompaniment group with ID {id} not found" });

        return Ok(group);
    }

    [HttpPut("groups/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateGroup(Guid id, UpdateAccompanimentGroupDto dto)
    {
        await accompanimentService.UpdateGroupAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("groups/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteGroup(Guid id)
    {
        await accompanimentService.DeleteGroupAsync(id);
        return NoContent();
    }

    [HttpPost("groups/{groupId}/accompaniments")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<AccompanimentDto>> AddAccompaniment(Guid groupId, CreateAccompanimentDto dto)
    {
        var accompaniment = await accompanimentService.AddAccompanimentAsync(groupId, dto);
        return Ok(accompaniment);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AccompanimentDto>> GetAccompaniment(Guid id)
    {
        var accompaniment = await accompanimentService.GetAccompanimentByIdAsync(id);
        if (accompaniment == null)
            return NotFound(new { message = $"Accompaniment with ID {id} not found" });

        return Ok(accompaniment);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateAccompaniment(Guid id, UpdateAccompanimentDto dto)
    {
        await accompanimentService.UpdateAccompanimentAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteAccompaniment(Guid id)
    {
        await accompanimentService.DeleteAccompanimentAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/toggle-availability")]
    [Authorize(Roles = "Admin,Manager,Waiter")]
    public async Task<ActionResult<object>> ToggleAvailability(Guid id)
    {
        var newStatus = await accompanimentService.ToggleAvailabilityAsync(id);
        return Ok(new { id, isAvailable = newStatus });
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ValidationResult>> ValidateSelection([FromBody] ValidateSelectionRequest request)
    {
        var result = await accompanimentService.ValidateSelectionAsync(request.ProductId, request.SelectedAccompanimentIds);
        return Ok(result);
    }

    [HttpPost("calculate-charges")]
    public async Task<ActionResult<decimal>> CalculateCharges([FromBody] List<Guid> accompanimentIds)
    {
        var totalCharge = await accompanimentService.CalculateTotalExtraChargesAsync(accompanimentIds);
        return Ok(new { totalExtraCharge = totalCharge });
    }
}

public class ValidateSelectionRequest
{
    public Guid ProductId { get; set; }
    public List<Guid> SelectedAccompanimentIds { get; set; } = new();
}
