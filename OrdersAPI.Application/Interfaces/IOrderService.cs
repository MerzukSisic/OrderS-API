using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(Guid waiterId, CreateOrderDto dto);
    Task<OrderDto> GetOrderByIdAsync(Guid id);
    Task<IEnumerable<OrderDto>> GetOrdersAsync(Guid? waiterId = null, DateTime? fromDate = null, DateTime? toDate = null, OrderStatus? status = null);
    Task UpdateOrderStatusAsync(Guid id, OrderStatus status);
    Task<IEnumerable<OrderDto>> GetActiveOrdersAsync();
    
    Task UpdateOrderItemStatusAsync(Guid orderItemId, OrderItemStatus status);
    Task CancelOrderAsync(Guid orderId, string reason);
    Task<OrderItemDto> AddItemToOrderAsync(Guid orderId, CreateOrderItemDto dto);
    Task<List<OrderDto>> GetOrdersByTableAsync(Guid tableId);
    Task CompleteOrderAsync(Guid orderId);
    Task<List<OrderItemDto>> GetOrderItemsByLocationAsync(PreparationLocation location, OrderItemStatus? status = null);
}
