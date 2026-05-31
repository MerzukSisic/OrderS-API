using OrdersAPI.Domain.Exceptions;
﻿using Microsoft.EntityFrameworkCore;
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
    public async Task<PagedResult<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, int page = 1, int pageSize = 50)
    {
        var clampedPageSize = Math.Min(pageSize, 100);
        var query = context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var totalCount = await query.CountAsync();

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * clampedPageSize)
            .Take(clampedPageSize)
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

        return new PagedResult<NotificationDto> { Items = notifications, TotalCount = totalCount, Page = page, PageSize = clampedPageSize };
    }

    public async Task<NotificationDto> CreateNotificationAsync(Guid userId, string title, string message, string type)
    {
        var userExists = await context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            throw new NotFoundException($"User with ID {userId} not found");

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

    public async Task MarkAsReadAsync(Guid notificationId, Guid actorUserId, bool isAdmin = false)
    {
        var notification = await context.Notifications.FindAsync(notificationId);
        if (notification == null)
            throw new NotFoundException($"Notification with ID {notificationId} not found");

        // Fix 8: Ownership provjera - samo vlasnik ili admin mogu mijenjati notifikaciju
        if (!isAdmin && notification.UserId != actorUserId)
            throw new UnauthorizedAccessException("You can only mark your own notifications as read");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync();

            logger.LogInformation("Notification {NotificationId} marked as read by user {UserId}", notificationId, actorUserId);
        }
    }

    public async Task DeleteNotificationAsync(Guid notificationId, Guid actorUserId, bool isAdmin = false)
    {
        var notification = await context.Notifications.FindAsync(notificationId);
        if (notification == null)
            throw new NotFoundException($"Notification with ID {notificationId} not found");

        // Fix 8: Ownership provjera - samo vlasnik ili admin mogu brisati notifikaciju
        if (!isAdmin && notification.UserId != actorUserId)
            throw new UnauthorizedAccessException("You can only delete your own notifications");

        context.Notifications.Remove(notification);
        await context.SaveChangesAsync();

        logger.LogInformation("Notification {NotificationId} deleted by user {UserId}", notificationId, actorUserId);
    }

    public async Task CreateSystemNotificationAsync(Guid userId, string title, string message, string type)
    {
        var userExists = await context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists) return;

        if (!Enum.TryParse<NotificationType>(type, ignoreCase: true, out var notificationType))
            notificationType = NotificationType.Info;

        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = notificationType,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
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
