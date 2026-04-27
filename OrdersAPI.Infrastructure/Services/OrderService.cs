using OrdersAPI.Domain.Exceptions;
﻿using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;
using OrdersAPI.Infrastructure.Hubs;
using OrdersAPI.Domain.Events;

namespace OrdersAPI.Infrastructure.Services;

public class OrderService(
    ApplicationDbContext context,
    IAccompanimentService accompanimentService,
    IPublishEndpoint publishEndpoint,
    IHubContext<OrderHub> hubContext, 
    ILogger<OrderService> logger)
    : IOrderService
{
    public async Task<OrderDto> CreateOrderAsync(Guid waiterId, CreateOrderDto dto)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Validate waiter exists
            var waiterExists = await context.Users.AnyAsync(u => u.Id == waiterId && u.IsActive);
            if (!waiterExists)
                throw new NotFoundException($"Waiter with ID {waiterId} not found");

            // Validate table if DineIn
            if (dto.Type == "DineIn" && !dto.TableId.HasValue)
                throw new BusinessException("TableId is required for DineIn orders");

            if (dto.TableId.HasValue)
            {
                var table = await context.CafeTables.FindAsync(dto.TableId.Value);
                if (table == null)
                    throw new NotFoundException($"Table with ID {dto.TableId} not found");
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

            // Batch-load all products and admins before the loop to avoid N+1 queries
            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await context.Products
                .Include(p => p.ProductIngredients)
                    .ThenInclude(pi => pi.StoreProduct)
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted && !p.Category.IsDeleted)
                .ToListAsync();
            var productLookup = products.ToDictionary(p => p.Id);

            var adminUsers = await context.Users
                .Where(u => u.Role == UserRole.Admin && u.IsActive)
                .ToListAsync();

            var signalRNotifications = new List<(Guid orderItemId, string productName, PreparationLocation location, int quantity, string notes, DateTime createdAt)>();

            foreach (var itemDto in dto.Items)
            {
                if (!productLookup.TryGetValue(itemDto.ProductId, out var product) || !product.IsAvailable)
                    throw new BusinessException($"Product {itemDto.ProductId} is not available");

                if (product.Stock < itemDto.Quantity)
                    throw new BusinessException($"Insufficient stock for {product.Name}. Available: {product.Stock}");

                // Validate accompaniments
                if (itemDto.SelectedAccompanimentIds.Any())
                {
                    var validationResult = await accompanimentService.ValidateSelectionAsync(
                        product.Id, 
                        itemDto.SelectedAccompanimentIds
                    );

                    if (!validationResult.IsValid)
                        throw new BusinessException(
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

                // Add accompaniments sa PriceAtOrder
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
                            PriceAtOrder = accompaniment?.ExtraCharge ?? 0
                        });
                    }
                }

                order.Items.Add(orderItem);

                // ✅ Sačuvaj podatke za SignalR prije nego product izađe iz scope-a
                signalRNotifications.Add((
                    orderItem.Id,
                    product.Name,
                    product.Location,
                    orderItem.Quantity,
                    itemDto.Notes,
                    orderItem.CreatedAt
                )!);

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
                        foreach (var admin in adminUsers)
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

            // ✅ Učitaj waiter i table podatke PRIJE SaveChanges
            var waiter = await context.Users.FindAsync(waiterId);
            var tableInfo = order.TableId.HasValue 
                ? await context.CafeTables.FindAsync(order.TableId.Value) 
                : null;

            await context.SaveChangesAsync();

            // ✅ Pošalji SignalR notifikacije koristeći sačuvane podatke
            foreach (var notification in signalRNotifications)
            {
                var notificationData = new
                {
                    OrderItemId = notification.orderItemId,
                    OrderId = order.Id,
                    ProductName = notification.productName,
                    Quantity = notification.quantity,
                    TableNumber = tableInfo?.TableNumber,
                    WaiterName = waiter?.FullName,
                    OrderType = order.Type.ToString(),
                    IsPartnerOrder = order.IsPartnerOrder,
                    Notes = notification.notes,
                    CreatedAt = notification.createdAt
                };

                if (notification.location == PreparationLocation.Kitchen)
                {
                    await hubContext.Clients.Group("Kitchen").SendAsync("NewKitchenOrder", notificationData);
                    logger.LogInformation("📨 SignalR: Sent NewKitchenOrder notification for item {ItemId}", notification.orderItemId);
                }
                else if (notification.location == PreparationLocation.Bar)
                {
                    await hubContext.Clients.Group("Bartender").SendAsync("NewBarOrder", notificationData);
                    logger.LogInformation("📨 SignalR: Sent NewBarOrder notification for item {ItemId}", notification.orderItemId);
                }
            }

            await transaction.CommitAsync();

            // ✅ LOGIRAJ KOJE RAČUNE TREBA GENERISATI ZA OVU NARUDŽBU
            var hasKitchenItems = order.Items.Any(i => i.Product.Location == PreparationLocation.Kitchen);
            var hasBarItems = order.Items.Any(i => i.Product.Location == PreparationLocation.Bar);

            var receiptTypes = new List<string>();
            if (hasKitchenItems) receiptTypes.Add("Kitchen");
            if (hasBarItems) receiptTypes.Add("Bar");
            receiptTypes.Add("Customer"); // UVIJEK

            logger.LogInformation(
                "📄 Order {OrderId} receipts available: {ReceiptTypes}", 
                order.Id, string.Join(", ", receiptTypes));
            
            // Publish RabbitMQ event
            await publishEndpoint.Publish(new OrderCreatedEvent
            {
                OrderId = order.Id,
                WaiterId = order.WaiterId,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new OrderItemEvent
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
        });
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
            throw new NotFoundException($"Order with ID {id} not found");

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
            IsArchived = order.IsArchived,
            ArchivedAt = order.ArchivedAt,
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
                    ExtraCharge = oia.PriceAtOrder
                }).ToList()
            }).ToList()
        };
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(
        Guid? waiterId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        OrderStatus? status = null,
        int page = 1,
        int pageSize = 50)
    {
        var clampedPageSize = Math.Min(pageSize, 100);
        var query = context.Orders
            .AsNoTracking()
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => !o.IsArchived)
            .AsQueryable();

        if (waiterId.HasValue)
            query = query.Where(o => o.WaiterId == waiterId);

        if (status.HasValue)
            query = query.Where(o => o.Status == status);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate);

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToListAsync();

        var items = orders.Select(o => new OrderDto
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
            IsArchived = o.IsArchived,
            ArchivedAt = o.ArchivedAt,
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

        return new PagedResult<OrderDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = clampedPageSize };
    }

    public async Task UpdateOrderStatusAsync(Guid id, OrderStatus status)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == id);
            
        if (order == null)
            throw new NotFoundException($"Order with ID {id} not found");

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

    public async Task<PagedResult<OrderDto>> GetActiveOrdersAsync(int page = 1, int pageSize = 50)
    {
        var clampedPageSize = Math.Min(pageSize, 100);
        var query = context.Orders
            .AsNoTracking()
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => !o.IsArchived && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((page - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToListAsync();

        var items = orders.Select(o => new OrderDto
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
            IsArchived = o.IsArchived,
            ArchivedAt = o.ArchivedAt,
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

        return new PagedResult<OrderDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = clampedPageSize };
    }

    public async Task UpdateOrderItemStatusAsync(Guid orderItemId, OrderItemStatus status)
    {
        var orderItem = await context.OrderItems
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);
            
        if (orderItem == null)
            throw new NotFoundException($"Order item with ID {orderItemId} not found");

        var previousStatus = orderItem.Status;
        orderItem.Status = status;

        var order = orderItem.Order;

        // Subtract cancelled item from order total (only once, if not already cancelled)
        if (status == OrderItemStatus.Cancelled && previousStatus != OrderItemStatus.Cancelled)
        {
            order.TotalAmount = Math.Max(0, order.TotalAmount - orderItem.Subtotal);
            order.UpdatedAt = DateTime.UtcNow;
        }

        // ✅ UPDATE ORDER STATUS BASED ON ALL ITEMS
        
        // Get all item statuses (including the one we're updating)
        var allItemsStatus = order.Items
            .Select(i => i.Id == orderItemId ? status : i.Status)
            .ToList();
        
        // Determine new order status based on all items
        // Check if all non-cancelled items are ready
        var activeItems = allItemsStatus.Where(s => s != OrderItemStatus.Cancelled).ToList();
        
        if (allItemsStatus.All(s => s == OrderItemStatus.Cancelled))
        {
            // All items cancelled → Cancel order
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            logger.LogInformation("✅ Order {OrderId} status automatically set to Cancelled (all items cancelled)", order.Id);
        }
        else if (activeItems.Any() && activeItems.All(s => s == OrderItemStatus.Ready))
        {
            // All active items ready → Order ready
            order.Status = OrderStatus.Ready;
            order.UpdatedAt = DateTime.UtcNow;
            logger.LogInformation("✅ Order {OrderId} status automatically set to Ready (all active items ready)", order.Id);
        }
        else if (activeItems.Any(s => s == OrderItemStatus.Preparing))
        {
            // Some items preparing → Order preparing
            order.Status = OrderStatus.Preparing;
            order.UpdatedAt = DateTime.UtcNow;
            logger.LogInformation("✅ Order {OrderId} status automatically set to Preparing (some items preparing)", order.Id);
        }
        else if (activeItems.Any() && activeItems.All(s => s == OrderItemStatus.Pending))
        {
            // All active items pending → Order pending
            order.Status = OrderStatus.Pending;
            order.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        // ✅ Notify all clients about status change (both item and order)
        await hubContext.Clients.All.SendAsync("OrderItemStatusChanged", new
        {
            OrderItemId = orderItemId,
            Status = status.ToString(),
            OrderId = order.Id,
            OrderStatus = order.Status.ToString(),
            ChangedAt = DateTime.UtcNow
        });

        logger.LogInformation("Order item {OrderItemId} status updated to {Status}", orderItemId, status);
        logger.LogInformation("Order {OrderId} status is now {Status}", order.Id, order.Status);
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
            throw new NotFoundException($"Order with ID {orderId} not found");

        if (order.Status == OrderStatus.Completed)
        {
            await ArchiveOrderAsync(orderId);
            return;
        }

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

    public async Task ArchiveOrderAsync(Guid orderId)
    {
        var order = await context.Orders.FindAsync(orderId);
        if (order == null)
            throw new NotFoundException($"Order with ID {orderId} not found");

        if (order.Status != OrderStatus.Completed && order.Status != OrderStatus.Cancelled)
            throw new BusinessException("Only completed or cancelled orders can be archived");

        if (order.IsArchived)
            return;

        order.IsArchived = true;
        order.ArchivedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("Order {OrderId} archived", orderId);
    }

    public async Task<OrderItemDto> AddItemToOrderAsync(Guid orderId, CreateOrderItemDto dto)
    {
        var order = await context.Orders
            .Include(o => o.Items)
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new NotFoundException($"Order with ID {orderId} not found");

        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
            throw new BusinessException("Cannot add items to completed or cancelled order");

        var product = await context.Products
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId && !p.IsDeleted && !p.Category.IsDeleted);

        if (product == null || !product.IsAvailable)
            throw new BusinessException($"Product {dto.ProductId} is not available");

        if (product.Stock < dto.Quantity)
            throw new BusinessException($"Insufficient stock for {product.Name}");

        // Validate accompaniments
        if (dto.SelectedAccompanimentIds.Any())
        {
            var validationResult = await accompanimentService.ValidateSelectionAsync(
                product.Id, 
                dto.SelectedAccompanimentIds
            );

            if (!validationResult.IsValid)
                throw new BusinessException(
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

        // Add accompaniments sa PriceAtOrder
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

        // ✅ Sačuvaj podatke PRIJE SaveChanges
        var productName = product.Name;
        var productLocation = product.Location;
        var waiterName = order.Waiter.FullName;
        var tableNumber = order.Table?.TableNumber;

        await context.SaveChangesAsync();

        // ✅ Pošalji SignalR notifikaciju sa sačuvanim podacima
        var notificationData = new
        {
            OrderItemId = orderItem.Id,
            OrderId = orderId,
            ProductName = productName,
            Quantity = orderItem.Quantity,
            TableNumber = tableNumber,
            WaiterName = waiterName,
            OrderType = order.Type.ToString(),
            IsPartnerOrder = order.IsPartnerOrder,
            Notes = orderItem.Notes,
            CreatedAt = orderItem.CreatedAt
        };

        if (productLocation == PreparationLocation.Kitchen)
        {
            await hubContext.Clients.Group("Kitchen").SendAsync("NewKitchenOrder", notificationData);
            logger.LogInformation("📨 SignalR: Sent NewKitchenOrder notification for added item {ItemId}", orderItem.Id);
        }
        else if (productLocation == PreparationLocation.Bar)
        {
            await hubContext.Clients.Group("Bartender").SendAsync("NewBarOrder", notificationData);
            logger.LogInformation("📨 SignalR: Sent NewBarOrder notification for added item {ItemId}", orderItem.Id);
        }

        logger.LogInformation("Item added to order {OrderId}: {ProductId} x{Quantity}", 
            orderId, dto.ProductId, dto.Quantity);

        return new OrderItemDto
        {
            Id = orderItem.Id,
            ProductId = orderItem.ProductId,
            ProductName = productName,
            PreparationLocation = productLocation.ToString(),
            Quantity = orderItem.Quantity,
            UnitPrice = orderItem.UnitPrice,
            Subtotal = orderItem.Subtotal,
            Notes = orderItem.Notes,
            Status = orderItem.Status.ToString(),
            CreatedAt = orderItem.CreatedAt
        };
    }

    public async Task<PagedResult<OrderDto>> GetOrdersByTableAsync(Guid tableId, int page = 1, int pageSize = 50)
    {
        var clampedPageSize = Math.Min(pageSize, 100);
        var query = context.Orders
            .AsNoTracking()
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => o.TableId == tableId && !o.IsArchived)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((page - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToListAsync();

        var items = orders.Select(o => new OrderDto
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
            IsArchived = o.IsArchived,
            ArchivedAt = o.ArchivedAt,
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

        return new PagedResult<OrderDto> { Items = items, TotalCount = totalCount, Page = page, PageSize = clampedPageSize };
    }

    public async Task CompleteOrderAsync(Guid orderId)
    {
        await UpdateOrderStatusAsync(orderId, OrderStatus.Completed);
    }

    public async Task<PagedResult<OrderItemDto>> GetOrderItemsByLocationAsync(PreparationLocation location, OrderItemStatus? status = null, int page = 1, int pageSize = 100)
    {
        var clampedPageSize = Math.Min(pageSize, 200);
        var query = context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Product)
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Waiter)
            .Include(oi => oi.Order)
                .ThenInclude(o => o.Table)
            .Include(oi => oi.OrderItemAccompaniments)
                .ThenInclude(oia => oia.Accompaniment)
            .Where(oi => oi.Product.Location == location)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(oi => oi.Status == status.Value);
        else
            query = query.Where(oi => oi.Status == OrderItemStatus.Pending || oi.Status == OrderItemStatus.Preparing);

        var totalCount = await query.CountAsync();
        var rawItems = await query
            .OrderBy(oi => oi.CreatedAt)
            .Skip((page - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .ToListAsync();

        var dtoItems = rawItems.Select(i => new OrderItemDto
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
                ExtraCharge = oia.PriceAtOrder
            }).ToList()
        }).ToList();

        return new PagedResult<OrderItemDto> { Items = dtoItems, TotalCount = totalCount, Page = page, PageSize = clampedPageSize };
    }
}
