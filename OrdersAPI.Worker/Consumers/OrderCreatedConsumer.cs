using MassTransit;
using OrdersAPI.Domain.Events;

namespace OrdersAPI.Worker.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        try
        {
            var message = context.Message;
            
            logger.LogInformation(
                "🚀 WORKER: Processing OrderCreated - Order #{OrderNumber} (ID: {OrderId})",
                message.OrderNumber,
                message.OrderId);

            logger.LogInformation(
                "📋 Order Details: Table {Table}, Waiter {Waiter}, Total {Total:C}, Items: {ItemCount}",
                message.TableNumber?.ToString() ?? "Takeaway",
                message.WaiterName,
                message.TotalAmount,
                message.Items.Count);

            var kitchenItems = message.Items.Where(i => i.PreparationLocation == "Kitchen").ToList();
            var barItems = message.Items.Where(i => i.PreparationLocation == "Bar").ToList();

            if (kitchenItems.Any())
            {
                logger.LogInformation("🍳 Kitchen Items ({Count}):", kitchenItems.Count);
                foreach (var item in kitchenItems)
                {
                    logger.LogInformation("   - {Product} x{Qty}", item.ProductName, item.Quantity);
                }
            }

            if (barItems.Any())
            {
                logger.LogInformation("🍺 Bar Items ({Count}):", barItems.Count);
                foreach (var item in barItems)
                {
                    logger.LogInformation("   - {Product} x{Qty}", item.ProductName, item.Quantity);
                }
            }

            await Task.Delay(100);

            logger.LogInformation(
                "✅ WORKER: Successfully processed Order #{OrderNumber}",
                message.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "❌ WORKER: Error processing Order {OrderId}", 
                context.Message.OrderId);
            throw;
        }
    }
}