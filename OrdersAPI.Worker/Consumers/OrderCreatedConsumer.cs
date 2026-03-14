using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Domain.Events;
using OrdersAPI.Worker.Data;

namespace OrdersAPI.Worker.Consumers;

public class OrderCreatedConsumer(ILogger<OrderCreatedConsumer> logger, WorkerDbContext db) : IConsumer<OrderCreatedEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        try
        {
            var message = context.Message;

            logger.LogInformation(
                "WORKER: Processing OrderCreated - Order #{OrderNumber} (ID: {OrderId})",
                message.OrderNumber,
                message.OrderId);

            var kitchenItems = message.Items.Where(i => i.PreparationLocation == "Kitchen").ToList();
            var barItems = message.Items.Where(i => i.PreparationLocation == "Bar").ToList();

            var tableInfo = message.TableNumber.HasValue
                ? $"Table {message.TableNumber}"
                : "Takeaway";

            var notifications = new List<Notification>();

            if (kitchenItems.Any())
            {
                var kitchenStaff = await db.Users
                    .Where(u => u.IsActive && u.Role == UserRole.Kitchen)
                    .ToListAsync();

                var itemSummary = string.Join(", ", kitchenItems.Select(i => $"{i.ProductName} x{i.Quantity}"));
                foreach (var staff in kitchenStaff)
                {
                    notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = staff.Id,
                        Title = $"New Order #{message.OrderNumber} - {tableInfo}",
                        Message = $"Kitchen items: {itemSummary}",
                        Type = NotificationType.Info,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
            }

            if (barItems.Any())
            {
                var barStaff = await db.Users
                    .Where(u => u.IsActive && u.Role == UserRole.Bartender)
                    .ToListAsync();

                var itemSummary = string.Join(", ", barItems.Select(i => $"{i.ProductName} x{i.Quantity}"));
                foreach (var staff in barStaff)
                {
                    notifications.Add(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = staff.Id,
                        Title = $"New Order #{message.OrderNumber} - {tableInfo}",
                        Message = $"Bar items: {itemSummary}",
                        Type = NotificationType.Info,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                    });
                }
            }

            if (notifications.Count > 0)
            {
                db.Notifications.AddRange(notifications);
                await db.SaveChangesAsync();

                logger.LogInformation(
                    "WORKER: Created {Count} notifications for Order #{OrderNumber}",
                    notifications.Count,
                    message.OrderNumber);
            }

            logger.LogInformation(
                "WORKER: Successfully processed Order #{OrderNumber}",
                message.OrderNumber);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "WORKER: Error processing Order {OrderId}",
                context.Message.OrderId);
            throw;
        }
    }
}