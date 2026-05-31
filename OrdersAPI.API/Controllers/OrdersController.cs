using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Constants;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.AdminManagerOrWaiter)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await orderService.CreateOrderAsync(userId, dto);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        var order = await orderService.GetOrderByIdAsync(id);
        return Ok(order);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
        [FromQuery] Guid? waiterId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await orderService.GetOrdersAsync(waiterId, fromDate, toDate, status, page, pageSize);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page"] = result.Page.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result.Items);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetActiveOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await orderService.GetActiveOrdersAsync(page, pageSize);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page"] = result.Page.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result.Items);
    }

    [HttpGet("table/{tableId}")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByTable(
        Guid tableId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await orderService.GetOrdersByTableAsync(tableId, page, pageSize);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page"] = result.Page.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result.Items);
    }

    [HttpGet("items/by-location")]
    [Authorize(Roles = Roles.KitchenOrBar)]
    public async Task<ActionResult<IEnumerable<OrderItemDto>>> GetOrderItemsByLocation(
        [FromQuery] string location,
        [FromQuery] OrderItemStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
    {
        // Fix 18: Sigurno parsiranje enum-a
        if (!Enum.TryParse<PreparationLocation>(location, ignoreCase: true, out var preparationLocation))
            return BadRequest($"Invalid location '{location}'. Valid values: {string.Join(", ", Enum.GetNames<PreparationLocation>())}");

        var result = await orderService.GetOrderItemsByLocationAsync(preparationLocation, status, page, pageSize);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page"] = result.Page.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return Ok(result.Items);
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        // Fix 18: Sigurno parsiranje enum-a
        if (!Enum.TryParse<OrderStatus>(dto.Status, ignoreCase: true, out var status))
            return BadRequest($"Invalid status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}");

        await orderService.UpdateOrderStatusAsync(id, status, CurrentUserId(), CurrentUserRole());
        return NoContent();
    }

    [HttpPut("{id}/complete")]
    [Authorize(Roles = Roles.AdminManagerOrWaiter)]
    public async Task<IActionResult> CompleteOrder(Guid id)
    {
        await orderService.CompleteOrderAsync(id, CurrentUserId(), CurrentUserRole());
        return NoContent();
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Roles = Roles.AdminManagerOrWaiter)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderDto dto)
    {
        await orderService.CancelOrderAsync(id, dto.Reason, CurrentUserId(), CurrentUserRole());
        return NoContent();
    }

    [HttpPut("{id}/archive")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> ArchiveOrder(Guid id)
    {
        await orderService.ArchiveOrderAsync(id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        await orderService.ArchiveOrderAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/items")]
    [Authorize(Roles = Roles.AdminManagerOrWaiter)]
    public async Task<ActionResult<OrderItemDto>> AddItemToOrder(Guid id, [FromBody] CreateOrderItemDto dto)
    {
        var item = await orderService.AddItemToOrderAsync(id, dto, CurrentUserId(), CurrentUserRole());
        return Ok(item);
    }

    [HttpPut("items/{itemId}/status")]
    [Authorize(Roles = Roles.AllStaff)]
    public async Task<IActionResult> UpdateOrderItemStatus(Guid itemId, [FromBody] UpdateOrderItemStatusDto dto)
    {
        // Fix 18: Sigurno parsiranje enum-a
        if (!Enum.TryParse<OrderItemStatus>(dto.Status, ignoreCase: true, out var status))
            return BadRequest($"Invalid status '{dto.Status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderItemStatus>())}");

        await orderService.UpdateOrderItemStatusAsync(itemId, status, CurrentUserId(), CurrentUserRole());
        return NoContent();
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private UserRole CurrentUserRole()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == null || !Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed))
            throw new UnauthorizedAccessException("Invalid or missing user role");

        return parsed;
    }
}
