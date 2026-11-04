using Ticketing.Domain.Entities;

namespace Ticketing.Domain.Contracts;

public interface INotificationService
{
    Task CreateNotificationAsync(int userId, string title, string message, string type = "info", string? targetUrl = null);
    Task<IReadOnlyList<TicketingNotification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int limit = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task<bool> MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
}
