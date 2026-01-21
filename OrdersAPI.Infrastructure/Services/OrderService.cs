using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;
using OrdersAPI.Infrastructure.Messaging.Events;

namespace OrdersAPI.Infrastructure.Services;

public class OrderService(
    ApplicationDbContext context,
    IAccompanimentService accompanimentService,
    IPublishEndpoint publishEndpoint,
    ILogger<OrderService> logger)
    : IOrderService
{
    public async Task<OrderDto> CreateOrderAsync(Guid waiterId, CreateOrderDto dto)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        
        try
        {
            // Validate waiter exists
            var waiterExists = await context.Users.AnyAsync(u => u.Id == waiterId && u.IsActive);
            if (!waiterExists)
                throw new KeyNotFoundException($"Waiter with ID {waiterId} not found");

            // Validate table if DineIn
            if (dto.Type == "DineIn" && !dto.TableId.HasValue)
                throw new InvalidOperationException("TableId is required for DineIn orders");

            if (dto.TableId.HasValue)
            {
                var table = await context.CafeTables.FindAsync(dto.TableId.Value);
                if (table == null)
                    throw new KeyNotFoundException($"Table with ID {dto.TableId} not found");
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                WaiterId = waiterId,
                TableId = dto.TableId,
                Type = Enum.Parse<OrderType>(dto.Type),
                IsPartnerOrder = dto.IsPartnerOrder,
                Notes = dto.Notes,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            decimal totalAmount = 0;

            foreach (var itemDto in dto.Items)
            {
                var product = await context.Products
                    .Include(p => p.ProductIngredients)
                        .ThenInclude(pi => pi.StoreProduct)
                    .FirstOrDefaultAsync(p => p.Id == itemDto.ProductId);

                if (product == null || !product.IsAvailable)
                    throw new InvalidOperationException($"Product {itemDto.ProductId} is not available");

                if (product.Stock < itemDto.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}. Available: {product.Stock}");

                // Validate accompaniments
                if (itemDto.SelectedAccompanimentIds.Any())
                {
                    var validationResult = await accompanimentService.ValidateSelectionAsync(
                        product.Id, 
                        itemDto.SelectedAccompanimentIds
                    );

                    if (!validationResult.IsValid)
                        throw new InvalidOperationException(
                            $"Accompaniment validation failed: {string.Join(", ", validationResult.Errors)}"
                        );
                }

                var accompanimentCharges = await accompanimentService.CalculateTotalExtraChargesAsync(
                    itemDto.SelectedAccompanimentIds
                );

                var itemPrice = product.Price + accompanimentCharges;
                var subtotal = itemPrice * itemDto.Quantity;
                totalAmount += subtotal;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemPrice,
                    Subtotal = subtotal,
                    Notes = itemDto.Notes,
                    Status = OrderItemStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                // ✅ FIX: Add accompaniments sa PriceAtOrder
                if (itemDto.SelectedAccompanimentIds.Any())
                {
                    var accompaniments = await context.Accompaniments
                        .AsNoTracking()
                        .Where(a => itemDto.SelectedAccompanimentIds.Contains(a.Id))
                        .ToListAsync();

                    foreach (var accompanimentId in itemDto.SelectedAccompanimentIds)
                    {
                        var accompaniment = accompaniments.FirstOrDefault(a => a.Id == accompanimentId);
                        
                        orderItem.OrderItemAccompaniments.Add(new OrderItemAccompaniment
                        {
                            Id = Guid.NewGuid(),
                            AccompanimentId = accompanimentId,
                            PriceAtOrder = accompaniment?.ExtraCharge ?? 0 // ✅ DODANO
                        });
                    }
                }

                order.Items.Add(orderItem);

                // Reduce product stock
                product.Stock -= itemDto.Quantity;

                // Reduce ingredient stock
                foreach (var ingredient in product.ProductIngredients)
                {
                    var requiredQty = ingredient.Quantity * itemDto.Quantity;
                    ingredient.StoreProduct.CurrentStock -= (int)requiredQty;

                    context.InventoryLogs.Add(new InventoryLog
                    {
                        Id = Guid.NewGuid(),
                        StoreProductId = ingredient.StoreProductId,
                        QuantityChange = -(int)requiredQty,
                        Type = InventoryLogType.Sale,
                        Reason = $"Order {order.Id}",
                        CreatedAt = DateTime.UtcNow
                    });

                    // Low stock notification
                    if (ingredient.StoreProduct.CurrentStock < ingredient.StoreProduct.MinimumStock)
                    {
                        var admins = await context.Users
                            .Where(u => u.Role == UserRole.Admin && u.IsActive)
                            .ToListAsync();

                        foreach (var admin in admins)
                        {
                            context.Notifications.Add(new Notification
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
            context.Orders.Add(order);

            // Update table status
            if (order.TableId.HasValue)
            {
                var table = await context.CafeTables.FindAsync(order.TableId.Value);
                if (table != null)
                    table.Status = TableStatus.Occupied;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Publish RabbitMQ event
            await publishEndpoint.Publish(new OrderCreatedEvent
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

            logger.LogInformation("Order {OrderId} created successfully with {ItemCount} items", 
                order.Id, order.Items.Count);

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
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.OrderItemAccompaniments)
                    .ThenInclude(oia => oia.Accompaniment)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new KeyNotFoundException($"Order with ID {id} not found");

        return new OrderDto
        {
            Id = order.Id,
            WaiterId = order.WaiterId,
            WaiterName = order.Waiter.FullName,
            TableId = order.TableId,
            TableNumber = order.Table?.TableNumber,
            Status = order.Status.ToString(),
            Type = order.Type.ToString(),
            IsPartnerOrder = order.IsPartnerOrder,
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CompletedAt = order.CompletedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                PreparationLocation = i.Product.Location.ToString(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                Notes = i.Notes,
                Status = i.Status.ToString(),
                CreatedAt = i.CreatedAt,
                SelectedAccompaniments = i.OrderItemAccompaniments.Select(oia => new SelectedAccompanimentDto
                {
                    AccompanimentId = oia.AccompanimentId,
                    Name = oia.Accompaniment.Name,
                    ExtraCharge = oia.PriceAtOrder // ✅ Koristi PriceAtOrder umjesto Accompaniment.ExtraCharge
                }).ToList()
            }).ToList()
        };
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(
        Guid? waiterId = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        OrderStatus? status = null)
    {
        var query = context.Orders
            .AsNoTracking()
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

        return orders.Select(o => new OrderDto
        {
            Id = o.Id,
            WaiterId = o.WaiterId,
            WaiterName = o.Waiter.FullName,
            TableId = o.TableId,
            TableNumber = o.Table?.TableNumber,
            Status = o.Status.ToString(),
            Type = o.Type.ToString(),
            IsPartnerOrder = o.IsPartnerOrder,
            TotalAmount = o.TotalAmount,
            Notes = o.Notes,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            CompletedAt = o.CompletedAt,
            Items = o.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                PreparationLocation = i.Product.Location.ToString(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                Notes = i.Notes,
                Status = i.Status.ToString(),
                CreatedAt = i.CreatedAt
            }).ToList()
        }).ToList();
    }

    public async Task UpdateOrderStatusAsync(Guid id, OrderStatus status)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == id);
            
        if (order == null)
            throw new KeyNotFoundException($"Order with ID {id} not found");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == OrderStatus.Completed)
        {
            order.CompletedAt = DateTime.UtcNow;
            
            foreach (var item in order.Items)
            {
                if (item.Status != OrderItemStatus.Cancelled)
                    item.Status = OrderItemStatus.Completed;
            }
            
            if (order.TableId.HasValue && order.Table != null)
            {
                var hasOtherActiveOrders = await context.Orders
                    .AnyAsync(o => 
                        o.TableId == order.TableId && 
                        o.Id != id && 
                        o.Status != OrderStatus.Completed && 
                        o.Status != OrderStatus.Cancelled);
                
                if (!hasOtherActiveOrders)
                    order.Table.Status = TableStatus.Available;
            }
        }
        
        if (status == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items)
            {
                item.Status = OrderItemStatus.Cancelled;
            }
            
            if (order.TableId.HasValue && order.Table != null)
            {
                var hasOtherActiveOrders = await context.Orders
                    .AnyAsync(o => 
                        o.TableId == order.TableId && 
                        o.Id != id && 
                        o.Status != OrderStatus.Completed && 
                        o.Status != OrderStatus.Cancelled);
                
                if (!hasOtherActiveOrders)
                    order.Table.Status = TableStatus.Available;
            }
        }

        await context.SaveChangesAsync();
        
        logger.LogInformation("Order {OrderId} status updated to {Status}", id, status);
    }

    public async Task<IEnumerable<OrderDto>> GetActiveOrdersAsync()
    {
        return await GetOrdersAsync(
            status: null, 
            waiterId: null, 
            fromDate: null, 
            toDate: null
        ).ContinueWith(t => t.Result.Where(o => 
            o.Status != OrderStatus.Completed.ToString() && 
            o.Status != OrderStatus.Cancelled.ToString()
        ));
    }

    public async Task UpdateOrderItemStatusAsync(Guid orderItemId, OrderItemStatus status)
    {
        var orderItem = await context.OrderItems.FindAsync(orderItemId);
        if (orderItem == null)
            throw new KeyNotFoundException($"Order item with ID {orderItemId} not found");

        orderItem.Status = status;
        await context.SaveChangesAsync();

        logger.LogInformation("Order item {OrderItemId} status updated to {Status}", orderItemId, status);
    }

    public async Task CancelOrderAsync(Guid orderId, string reason)
    {
        var order = await context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.ProductIngredients)
                        .ThenInclude(pi => pi.StoreProduct)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order with ID {orderId} not found");

        if (order.Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel completed order");

        // Restore stock
        foreach (var item in order.Items)
        {
            if (item.Status != OrderItemStatus.Cancelled)
            {
                item.Product.Stock += item.Quantity;

                foreach (var ingredient in item.Product.ProductIngredients)
                {
                    var restoredQty = ingredient.Quantity * item.Quantity;
                    ingredient.StoreProduct.CurrentStock += (int)restoredQty;

                    context.InventoryLogs.Add(new InventoryLog
                    {
                        Id = Guid.NewGuid(),
                        StoreProductId = ingredient.StoreProductId,
                        QuantityChange = (int)restoredQty,
                        Type = InventoryLogType.Adjustment,
                        Reason = $"Order {orderId} cancelled: {reason}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                item.Status = OrderItemStatus.Cancelled;
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;
        order.Notes = $"{order.Notes}\n[CANCELLED: {reason}]";

        // Free table
        if (order.TableId.HasValue && order.Table != null)
        {
            var hasOtherActiveOrders = await context.Orders
                .AnyAsync(o => 
                    o.TableId == order.TableId && 
                    o.Id != orderId && 
                    o.Status != OrderStatus.Completed && 
                    o.Status != OrderStatus.Cancelled);
            
            if (!hasOtherActiveOrders)
                order.Table.Status = TableStatus.Available;
        }

        await context.SaveChangesAsync();

        logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}", orderId, reason);
    }

    public async Task<OrderItemDto> AddItemToOrderAsync(Guid orderId, CreateOrderItemDto dto)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order with ID {orderId} not found");

        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot add items to completed or cancelled order");

        var product = await context.Products
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

        if (product == null || !product.IsAvailable)
            throw new InvalidOperationException($"Product {dto.ProductId} is not available");

        if (product.Stock < dto.Quantity)
            throw new InvalidOperationException($"Insufficient stock for {product.Name}");

        // ✅ FIX: Dodana accompaniment validation
        if (dto.SelectedAccompanimentIds.Any())
        {
            var validationResult = await accompanimentService.ValidateSelectionAsync(
                product.Id, 
                dto.SelectedAccompanimentIds
            );

            if (!validationResult.IsValid)
                throw new InvalidOperationException(
                    $"Accompaniment validation failed: {string.Join(", ", validationResult.Errors)}"
                );
        }

        var accompanimentCharges = await accompanimentService.CalculateTotalExtraChargesAsync(
            dto.SelectedAccompanimentIds
        );

        var itemPrice = product.Price + accompanimentCharges;
        var subtotal = itemPrice * dto.Quantity;

        var orderItem = new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = itemPrice,
            Subtotal = subtotal,
            Notes = dto.Notes,
            Status = OrderItemStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // ✅ FIX: Dodaj accompaniments sa PriceAtOrder
        if (dto.SelectedAccompanimentIds.Any())
        {
            var accompaniments = await context.Accompaniments
                .AsNoTracking()
                .Where(a => dto.SelectedAccompanimentIds.Contains(a.Id))
                .ToListAsync();

            foreach (var accompanimentId in dto.SelectedAccompanimentIds)
            {
                var accompaniment = accompaniments.FirstOrDefault(a => a.Id == accompanimentId);
                
                orderItem.OrderItemAccompaniments.Add(new OrderItemAccompaniment
                {
                    Id = Guid.NewGuid(),
                    AccompanimentId = accompanimentId,
                    PriceAtOrder = accompaniment?.ExtraCharge ?? 0
                });
            }
        }

        context.OrderItems.Add(orderItem);

        // Update stock
        product.Stock -= dto.Quantity;

        foreach (var ingredient in product.ProductIngredients)
        {
            var requiredQty = ingredient.Quantity * dto.Quantity;
            ingredient.StoreProduct.CurrentStock -= (int)requiredQty;
        }

        // Update order total
        order.TotalAmount += subtotal;
        order.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("Item added to order {OrderId}: {ProductId} x{Quantity}", 
            orderId, dto.ProductId, dto.Quantity);

        return new OrderItemDto
        {
            Id = orderItem.Id,
            ProductId = orderItem.ProductId,
            ProductName = product.Name,
            PreparationLocation = product.Location.ToString(),
            Quantity = orderItem.Quantity,
            UnitPrice = orderItem.UnitPrice,
            Subtotal = orderItem.Subtotal,
            Notes = orderItem.Notes,
            Status = orderItem.Status.ToString(),
            CreatedAt = orderItem.CreatedAt
        };
    }

    public async Task<List<OrderDto>> GetOrdersByTableAsync(Guid tableId)
    {
        var orders = await GetOrdersAsync();
        return orders.Where(o => o.TableId == tableId).ToList();
    }

    public async Task CompleteOrderAsync(Guid orderId)
    {
        await UpdateOrderStatusAsync(orderId, OrderStatus.Completed);
    }

    public async Task<List<OrderItemDto>> GetOrderItemsByLocationAsync(PreparationLocation location, OrderItemStatus? status = null)
    {
        var query = context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Product)
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Table)
            .Where(oi => oi.Product.Location == location)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(oi => oi.Status == status.Value);
        else
            query = query.Where(oi => oi.Status == OrderItemStatus.Pending || oi.Status == OrderItemStatus.Preparing);

        var items = await query
            .OrderBy(oi => oi.CreatedAt)
            .ToListAsync();

        return items.Select(i => new OrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            PreparationLocation = i.Product.Location.ToString(),
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Subtotal = i.Subtotal,
            Notes = i.Notes,
            Status = i.Status.ToString(),
            CreatedAt = i.CreatedAt
        }).ToList();
    }
}