using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Infrastructure.Hubs;
using OrdersAPI.Infrastructure.Messaging.Events;

namespace OrdersAPI.Infrastructure.Messaging.Consumers;

public class OrderCreatedConsumer(
    ILogger<OrderCreatedConsumer> logger,
    IHubContext<OrderHub> hubContext,
    INotificationService notificationService,
    IInventoryService inventoryService)
    : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        try
        {
            var message = context.Message;
            logger.LogInformation(
                "🚀 RabbitMQ: Processing OrderCreated - Order {OrderId}",
                message.OrderId);

            // 1. Send SignalR notifications
            await SendSignalRNotifications(message);

            // 2. Create system notifications (persistent)
            await CreateSystemNotifications(message);

            // 3. Update inventory (deduct ingredients)
            await UpdateInventory(message);

            logger.LogInformation(
                "✅ RabbitMQ: Successfully processed OrderCreated - Order {OrderId}",
                message.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "❌ RabbitMQ: Error processing OrderCreated - Order {OrderId}", 
                context.Message.OrderId);
            throw;
        }
    }

    private async Task SendSignalRNotifications(OrderCreatedEvent message)
    {
        try
        {
            // Group items by preparation location
            var kitchenItems = message.Items
                .Where(i => i.PreparationLocation == "Kitchen")
                .ToList();

            var barItems = message.Items
                .Where(i => i.PreparationLocation == "Bar")
                .ToList();

            // 🍳 Send to Kitchen
            if (kitchenItems.Any())
            {
                await hubContext.Clients.Group("Kitchen").SendAsync("NewKitchenOrder", new
                {
                    message.OrderId,
                    message.OrderNumber,
                    message.TableNumber,
                    message.WaiterName,
                    Items = kitchenItems.Select(i => new
                    {
                        i.ProductName,
                        i.Quantity,
                        i.Price
                    }),
                    message.CreatedAt
                });

                logger.LogInformation(
                    "🍳 SignalR: Sent {Count} items to Kitchen - Order {OrderId}",
                    kitchenItems.Count,
                    message.OrderId);
            }

            // 🍺 Send to Bar
            if (barItems.Any())
            {
                await hubContext.Clients.Group("Bartender").SendAsync("NewBarOrder", new
                {
                    message.OrderId,
                    message.OrderNumber,
                    message.TableNumber,
                    message.WaiterName,
                    Items = barItems.Select(i => new
                    {
                        i.ProductName,
                        i.Quantity,
                        i.Price
                    }),
                    message.CreatedAt
                });

                logger.LogInformation(
                    "🍺 SignalR: Sent {Count} items to Bar - Order {OrderId}",
                    barItems.Count,
                    message.OrderId);
            }

            // 👔 Send to Admins
            await hubContext.Clients.Group("Admin").SendAsync("OrderCreated", new
            {
                message.OrderId,
                message.OrderNumber,
                message.TableNumber,
                message.WaiterName,
                message.TotalAmount,
                message.OrderType,
                ItemCount = message.Items.Count,
                message.CreatedAt
            });

            logger.LogInformation(
                "👔 SignalR: Sent dashboard update to Admins - Order {OrderId}",
                message.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "SignalR: Error sending notifications for Order {OrderId}", 
                message.OrderId);
        }
    }

    private async Task CreateSystemNotifications(OrderCreatedEvent message)
    {
        try
        {
            var kitchenItemsCount = message.Items.Count(i => i.PreparationLocation == "Kitchen");
            var barItemsCount = message.Items.Count(i => i.PreparationLocation == "Bar");

            if (kitchenItemsCount > 0)
            {
                await notificationService.CreateNotificationAsync(
                    message.WaiterId,
                    "New Kitchen Order",
                    $"Order {message.OrderNumber} - Table {message.TableNumber ?? "Takeaway"} - {kitchenItemsCount} items",
                    "Info");
            }

            if (barItemsCount > 0)
            {
                await notificationService.CreateNotificationAsync(
                    message.WaiterId,
                    "New Bar Order",
                    $"Order {message.OrderNumber} - Table {message.TableNumber ?? "Takeaway"} - {barItemsCount} items",
                    "Info");
            }

            logger.LogInformation(
                "📬 Notifications: Created system notifications - Order {OrderId}",
                message.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Notifications: Error creating notifications for Order {OrderId}", 
                message.OrderId);
        }
    }

    private async Task UpdateInventory(OrderCreatedEvent message)
    {
        try
        {
            foreach (var item in message.Items)
            {
                await inventoryService.DeductIngredientsForOrderItemAsync(
                    item.ProductId, 
                    item.Quantity);

                logger.LogInformation(
                    "📦 Inventory: Deducted ingredients for {Product} x{Qty}",
                    item.ProductName,
                    item.Quantity);
            }

            logger.LogInformation(
                "📦 Inventory: Updated for Order {OrderId}",
                message.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Inventory: Error updating for Order {OrderId}", 
                message.OrderId);
        }
    }
}
