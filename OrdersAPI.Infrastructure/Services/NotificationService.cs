using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
    : INotificationService
{
    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false)
    {
        var query = context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return notifications;
    }

    public async Task<NotificationDto> CreateNotificationAsync(Guid userId, string title, string message, string type)
    {
        var userExists = await context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = Enum.Parse<NotificationType>(type),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        logger.LogInformation("Notification {NotificationId} created for user {UserId}: {Title}", 
            notification.Id, userId, title);

        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToString(),
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await context.Notifications.FindAsync(notificationId);
        if (notification == null)
            throw new KeyNotFoundException($"Notification with ID {notificationId} not found");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync();

            logger.LogInformation("Notification {NotificationId} marked as read", notificationId);
        }
    }

    public async Task DeleteNotificationAsync(Guid notificationId)
    {
        var notification = await context.Notifications.FindAsync(notificationId);
        if (notification == null)
            throw new KeyNotFoundException($"Notification with ID {notificationId} not found");

        context.Notifications.Remove(notification);
        await context.SaveChangesAsync();

        logger.LogInformation("Notification {NotificationId} deleted", notificationId);
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        var unreadCount = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));

        logger.LogInformation("{Count} notifications marked as read for user {UserId}", unreadCount, userId);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        var count = await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();

        return count;
    }

    public async Task DeleteReadNotificationsAsync(Guid userId)
    {
        var deletedCount = await context.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ExecuteDeleteAsync();

        logger.LogInformation("{Count} read notifications deleted for user {UserId}", deletedCount, userId);
    }
}
