using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Application.Service.Admin;

public class NotificationService : INotificationService
{
    private readonly TicketingDbContext _dbContext;
    private readonly ILogger<NotificationService>? _logger;

    public NotificationService(TicketingDbContext dbContext, ILogger<NotificationService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task CreateNotificationAsync(int userId, string title, string message, string type = "info", string? targetUrl = null)
    {
        try
        {
            var notif = new TicketingNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                TargetUrl = targetUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Notifications.AddAsync(notif);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create notification for user {UserId}", userId);
        }
    }

    public async Task<IReadOnlyList<TicketingNotification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int limit = 20)
    {
        try
        {
            var query = _dbContext.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to fetch notifications for user {UserId}, returning empty collection", userId);
            return Array.Empty<TicketingNotification>();
        }
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        try
        {
            return await _dbContext.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to count unread notifications for user {UserId}", userId);
            return 0;
        }
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        try
        {
            var notif = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notif == null)
                return false;

            notif.IsRead = true;
            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to mark notification {NotificationId} as read", notificationId);
            return false;
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        try
        {
            var unread = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to mark all notifications as read for user {UserId}", userId);
        }
    }
}
