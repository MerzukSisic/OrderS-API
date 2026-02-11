using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
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
        [FromQuery] OrderStatus? status = null)
    {
        var orders = await orderService.GetOrdersAsync(waiterId, fromDate, toDate, status);
        return Ok(orders);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetActiveOrders()
    {
        var orders = await orderService.GetActiveOrdersAsync();
        return Ok(orders);
    }

    [HttpGet("table/{tableId}")]
    public async Task<ActionResult<List<OrderDto>>> GetOrdersByTable(Guid tableId)
    {
        var orders = await orderService.GetOrdersByTableAsync(tableId);
        return Ok(orders);
    }

    [HttpGet("items/by-location")]
    [Authorize(Roles = "Admin,Bartender,Kitchen")]
    public async Task<ActionResult<List<OrderItemDto>>> GetOrderItemsByLocation(
        [FromQuery] string location,
        [FromQuery] OrderItemStatus? status = null)
    {
        var preparationLocation = Enum.Parse<PreparationLocation>(location);
        var items = await orderService.GetOrderItemsByLocationAsync(preparationLocation, status);
        return Ok(items);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        var status = Enum.Parse<OrderStatus>(dto.Status);
        await orderService.UpdateOrderStatusAsync(id, status);
        return NoContent();
    }

    [HttpPut("{id}/complete")]
    public async Task<IActionResult> CompleteOrder(Guid id)
    {
        await orderService.CompleteOrderAsync(id);
        return NoContent();
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderDto dto)
    {
        await orderService.CancelOrderAsync(id, dto.Reason);
        return NoContent();
    }

    [HttpPost("{id}/items")]
    public async Task<ActionResult<OrderItemDto>> AddItemToOrder(Guid id, [FromBody] CreateOrderItemDto dto)
    {
        var item = await orderService.AddItemToOrderAsync(id, dto);
        return Ok(item);
    }

    [HttpPut("items/{itemId}/status")]
    [Authorize(Roles = "Waiter,Bartender,Kitchen,Admin")]  
    public async Task<IActionResult> UpdateOrderItemStatus(Guid itemId, [FromBody] UpdateOrderItemStatusDto dto)
    {
        var status = Enum.Parse<OrderItemStatus>(dto.Status);
        await orderService.UpdateOrderItemStatusAsync(itemId, status);
        return NoContent();
    }
}
