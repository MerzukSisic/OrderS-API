using MassTransit;
using Microsoft.Extensions.Logging;
using OrdersAPI.Infrastructure.Messaging.Events;

namespace OrdersAPI.Infrastructure.Messaging.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedEvent>
{
    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        
        logger.LogInformation(
            "RabbitMQ: Order {OrderId} created with total {Total}",
            message.OrderId,
            message.TotalAmount
        );

        // Ovdje možeš dodati dodatnu logiku (npr. slanje na printer, notifikacije, itd.)
        
        return Task.CompletedTask;
    }
}
