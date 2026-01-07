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
    /// <summary>
    /// Dohvati sve accompaniment groups za određeni proizvod
    /// GET: api/accompaniments/product/{productId}
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<List<AccompanimentGroupDto>>> GetByProduct(Guid productId)
    {
        try
        {
            var groups = await accompanimentService.GetByProductIdAsync(productId);
            return Ok(groups);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Kreiraj novu accompaniment grupu za proizvod
    /// POST: api/accompaniments/groups
    /// </summary>
    [HttpPost("groups")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<AccompanimentGroupDto>> CreateGroup(CreateAccompanimentGroupDto dto)
    {
        try
        {
            var group = await accompanimentService.CreateGroupAsync(dto);
            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the accompaniment group", details = ex.Message });
        }
    }

    /// <summary>
    /// Dohvati specifičnu accompaniment grupu
    /// GET: api/accompaniments/groups/{id}
    /// </summary>
    [HttpGet("groups/{id}")]
    public async Task<ActionResult<AccompanimentGroupDto>> GetGroup(Guid id)
    {
        try
        {
            var group = await accompanimentService.GetGroupByIdAsync(id);
            if (group == null)
                return NotFound(new { message = $"Accompaniment group with ID {id} not found" });

            return Ok(group);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Ažuriraj accompaniment grupu
    /// PUT: api/accompaniments/groups/{id}
    /// </summary>
    [HttpPut("groups/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateGroup(Guid id, UpdateAccompanimentGroupDto dto)
    {
        try
        {
            await accompanimentService.UpdateGroupAsync(id, dto);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obriši accompaniment grupu
    /// DELETE: api/accompaniments/groups/{id}
    /// </summary>
    [HttpDelete("groups/{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteGroup(Guid id)
    {
        try
        {
            await accompanimentService.DeleteGroupAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Dodaj prilog u grupu
    /// POST: api/accompaniments/groups/{groupId}/accompaniments
    /// </summary>
    [HttpPost("groups/{groupId}/accompaniments")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<AccompanimentDto>> AddAccompaniment(Guid groupId, CreateAccompanimentDto dto)
    {
        try
        {
            var accompaniment = await accompanimentService.AddAccompanimentAsync(groupId, dto);
            return Ok(accompaniment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while adding the accompaniment", details = ex.Message });
        }
    }

    /// <summary>
    /// Dohvati specifičan prilog
    /// GET: api/accompaniments/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AccompanimentDto>> GetAccompaniment(Guid id)
    {
        try
        {
            var accompaniment = await accompanimentService.GetAccompanimentByIdAsync(id);
            if (accompaniment == null)
                return NotFound(new { message = $"Accompaniment with ID {id} not found" });

            return Ok(accompaniment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Ažuriraj prilog
    /// PUT: api/accompaniments/{id}
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateAccompaniment(Guid id, UpdateAccompanimentDto dto)
    {
        try
        {
            await accompanimentService.UpdateAccompanimentAsync(id, dto);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obriši prilog
    /// DELETE: api/accompaniments/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteAccompaniment(Guid id)
    {
        try
        {
            await accompanimentService.DeleteAccompanimentAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Toggle dostupnost priloga
    /// PATCH: api/accompaniments/{id}/toggle-availability
    /// </summary>
    [HttpPatch("{id}/toggle-availability")]
    [Authorize(Roles = "Admin,Manager,Waiter")]
    public async Task<ActionResult<object>> ToggleAvailability(Guid id)
    {
        try
        {
            var newStatus = await accompanimentService.ToggleAvailabilityAsync(id);
            return Ok(new { id, isAvailable = newStatus });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Validate selected accompaniments for a product
    /// POST: api/accompaniments/validate
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ValidationResult>> ValidateSelection([FromBody] ValidateSelectionRequest request)
    {
        try
        {
            var result = await accompanimentService.ValidateSelectionAsync(request.ProductId, request.SelectedAccompanimentIds);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Calculate total extra charges for accompaniments
    /// POST: api/accompaniments/calculate-charges
    /// </summary>
    [HttpPost("calculate-charges")]
    public async Task<ActionResult<decimal>> CalculateCharges([FromBody] List<Guid> accompanimentIds)
    {
        try
        {
            var totalCharge = await accompanimentService.CalculateTotalExtraChargesAsync(accompanimentIds);
            return Ok(new { totalExtraCharge = totalCharge });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

// Request DTOs for validation and calculation
public class ValidateSelectionRequest
{
    public Guid ProductId { get; set; }
    public List<Guid> SelectedAccompanimentIds { get; set; } = new();
}