using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class NotificationService(ApplicationDbContext context, IMapper mapper, ILogger<NotificationService> logger)
    : INotificationService
{
    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false)
    {
        var query = context.Notifications
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return mapper.Map<IEnumerable<NotificationDto>>(notifications);
    }

    // ✅ ISPRAVLJENO: string type umesto NotificationType type
    public async Task<NotificationDto> CreateNotificationAsync(Guid userId, string title, string message, string type)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = Enum.Parse<NotificationType>(type), // ✅ Parsira string u enum
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        return mapper.Map<NotificationDto>(notification);
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await context.Notifications.FindAsync(notificationId);
        if (notification == null)
            throw new KeyNotFoundException($"Notification {notificationId} not found");

        notification.IsRead = true;
        await context.SaveChangesAsync();
    }

    public async Task DeleteNotificationAsync(Guid notificationId)
    {
        var notification = await context.Notifications.FindAsync(notificationId);
        if (notification == null)
            throw new KeyNotFoundException($"Notification {notificationId} not found");

        context.Notifications.Remove(notification);
        await context.SaveChangesAsync();
    }
}
