using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false);
    Task<NotificationDto> CreateNotificationAsync(Guid userId, string title, string message, string type);
    Task MarkAsReadAsync(Guid notificationId);
    Task DeleteNotificationAsync(Guid notificationId);
}
