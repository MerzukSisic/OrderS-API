using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(Guid waiterId, CreateOrderDto dto);
    Task<OrderDto> GetOrderByIdAsync(Guid id);
    Task<PagedResult<OrderDto>> GetOrdersAsync(Guid? waiterId = null, DateTime? fromDate = null, DateTime? toDate = null, OrderStatus? status = null, int page = 1, int pageSize = 50);
    Task UpdateOrderStatusAsync(Guid id, OrderStatus status);
    Task UpdateOrderStatusAsync(Guid id, OrderStatus status, Guid actorUserId, UserRole actorRole);
    Task<PagedResult<OrderDto>> GetActiveOrdersAsync(int page = 1, int pageSize = 50);

    Task UpdateOrderItemStatusAsync(Guid orderItemId, OrderItemStatus status);
    Task UpdateOrderItemStatusAsync(Guid orderItemId, OrderItemStatus status, Guid actorUserId, UserRole actorRole);
    Task CancelOrderAsync(Guid orderId, string reason);
    Task CancelOrderAsync(Guid orderId, string reason, Guid actorUserId, UserRole actorRole);
    Task ArchiveOrderAsync(Guid orderId);
    Task<OrderItemDto> AddItemToOrderAsync(Guid orderId, CreateOrderItemDto dto);
    Task<OrderItemDto> AddItemToOrderAsync(Guid orderId, CreateOrderItemDto dto, Guid actorUserId, UserRole actorRole);
    Task<PagedResult<OrderDto>> GetOrdersByTableAsync(Guid tableId, int page = 1, int pageSize = 50);
    Task CompleteOrderAsync(Guid orderId);
    Task CompleteOrderAsync(Guid orderId, Guid actorUserId, UserRole actorRole);
    Task<PagedResult<OrderItemDto>> GetOrderItemsByLocationAsync(PreparationLocation location, OrderItemStatus? status = null, int page = 1, int pageSize = 100);
}
