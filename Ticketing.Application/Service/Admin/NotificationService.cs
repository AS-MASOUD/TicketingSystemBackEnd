using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Application.Service.Admin;

public class NotificationService : INotificationService
{
    private readonly TicketingDbContext _dbContext;

    public NotificationService(TicketingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task CreateNotificationAsync(int userId, string title, string message, string type = "info", string? targetUrl = null)
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

    public async Task<IReadOnlyList<TicketingNotification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false, int limit = 20)
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

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _dbContext.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
    {
        var notif = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notif == null)
            return false;

        notif.IsRead = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(int userId)
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
}
