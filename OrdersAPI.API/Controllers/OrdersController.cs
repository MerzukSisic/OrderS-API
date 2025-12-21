using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var order = await _orderService.CreateOrderAsync(userId, dto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(Guid id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
        [FromQuery] Guid? waiterId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] OrderStatus? status = null)
    {
        var orders = await _orderService.GetOrdersAsync(waiterId, fromDate, toDate, status);
        return Ok(orders);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetActiveOrders()
    {
        var orders = await _orderService.GetActiveOrdersAsync();
        return Ok(orders);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            var status = Enum.Parse<OrderStatus>(dto.Status);
            await _orderService.UpdateOrderStatusAsync(id, status);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
