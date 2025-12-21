using AutoMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;
using OrdersAPI.Infrastructure.Messaging.Events;

namespace OrdersAPI.Infrastructure.Services;

public class OrderService(
    ApplicationDbContext context,
    IMapper mapper,
    IPublishEndpoint publishEndpoint,
    ILogger<OrderService> logger)
    : IOrderService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

    public async Task<OrderDto> CreateOrderAsync(Guid waiterId, CreateOrderDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                WaiterId = waiterId,
                TableId = dto.TableId,
                Type = Enum.Parse<OrderType>(dto.Type),
                IsPartnerOrder = dto.IsPartnerOrder,
                Notes = dto.Notes,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            decimal totalAmount = 0;

            foreach (var itemDto in dto.Items)
            {
                // Učitaj proizvod sa sastojcima (EAGER LOADING - bez N+1)
                var product = await _context.Products
                    .Include(p => p.ProductIngredients)
                        .ThenInclude(pi => pi.StoreProduct)
                    .FirstOrDefaultAsync(p => p.Id == itemDto.ProductId);

                if (product == null || !product.IsAvailable)
                    throw new InvalidOperationException($"Product {itemDto.ProductId} not available");

                // Proveri zalihe proizvoda
                if (product.Stock < itemDto.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}");

                var subtotal = product.Price * itemDto.Quantity;
                totalAmount += subtotal;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    Subtotal = subtotal,
                    Notes = itemDto.Notes,
                    Status = OrderItemStatus.Pending
                };

                order.Items.Add(orderItem);

                // AUTOMATSKO SMANJENJE ZALIHA
                product.Stock -= itemDto.Quantity;

                // Smanji zalihe sastojaka
                foreach (var ingredient in product.ProductIngredients)
                {
                    var requiredQty = ingredient.Quantity * itemDto.Quantity;
                    ingredient.StoreProduct.CurrentStock -= (int)requiredQty;

                    // Log inventory change
                    _context.InventoryLogs.Add(new InventoryLog
                    {
                        Id = Guid.NewGuid(),
                        StoreProductId = ingredient.StoreProductId,
                        QuantityChange = -(int)requiredQty,
                        Type = InventoryLogType.Sale,
                        Reason = $"Order {order.Id}",
                        CreatedAt = DateTime.UtcNow
                    });

                    // LOW STOCK NOTIFICATION
                    if (ingredient.StoreProduct.CurrentStock < ingredient.StoreProduct.MinimumStock)
                    {
                        var admins = await _context.Users
                            .Where(u => u.Role == UserRole.Admin && u.IsActive)
                            .ToListAsync();

                        foreach (var admin in admins)
                        {
                            _context.Notifications.Add(new Notification
                            {
                                Id = Guid.NewGuid(),
                                UserId = admin.Id,
                                Title = "Low Stock Alert",
                                Message = $"{ingredient.StoreProduct.Name} is below minimum stock ({ingredient.StoreProduct.CurrentStock}/{ingredient.StoreProduct.MinimumStock})",
                                Type = NotificationType.LowStock,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }

            order.TotalAmount = totalAmount;
            _context.Orders.Add(order);

            // Ažuriraj status stola
            if (order.TableId.HasValue)
            {
                var table = await _context.CafeTables.FindAsync(order.TableId.Value);
                if (table != null)
                    table.Status = TableStatus.Occupied;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // PUBLISH RabbitMQ EVENT
            await _publishEndpoint.Publish(new OrderCreatedEvent
            {
                OrderId = order.Id,
                WaiterId = order.WaiterId,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new OrderItemData
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.UnitPrice
                }).ToList()
            });

            logger.LogInformation("Order {OrderId} created successfully", order.Id);

            return await GetOrderByIdAsync(order.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new KeyNotFoundException($"Order {id} not found");

        return mapper.Map<OrderDto>(order);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(
        Guid? waiterId = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        OrderStatus? status = null)
    {
        var query = _context.Orders
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (waiterId.HasValue)
            query = query.Where(o => o.WaiterId == waiterId);

        if (status.HasValue)
            query = query.Where(o => o.Status == status);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return mapper.Map<IEnumerable<OrderDto>>(orders);
    }

    public async Task UpdateOrderStatusAsync(Guid id, OrderStatus status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            throw new KeyNotFoundException($"Order {id} not found");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == OrderStatus.Completed)
            order.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        logger.LogInformation("Order {OrderId} status updated to {Status}", id, status);
    }

    public async Task<IEnumerable<OrderDto>> GetActiveOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return mapper.Map<IEnumerable<OrderDto>>(orders);
    }
}


