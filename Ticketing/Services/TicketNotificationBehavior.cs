using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.EndPoints.Ticket.Command.AddTicket;
using Ticketing.EndPoints.Ticket.Command.ChangeDevelopedBy;
using Ticketing.EndPoints.Ticket.Command.ChangeGroup;
using Ticketing.EndPoints.Ticket.Command.ChangeStatus;
using Ticketing.EndPoints.Ticket.Command.EditeTicket;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Services;

// The ticket mutation and its notifications commit together. Comparing database snapshots
// also covers handlers whose workflow SaveChanges persists a tracked ticket early.
public sealed class TicketNotificationBehavior<TRequest, TResponse>(TicketingDbContext db)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        int? id = request switch {
            AddTicketQuery => 0,
            ChangeStatusQuery q => q.TicketId,
            ChangeRoleQuery q => q.TicketId,
            DevelopedByQuery q => q.TicketId,
            EditeTicketQuery q => q.TicketId,
            _ => null
        };
        if (id == null) return await next();
        await using var transaction = db.Database.CurrentTransaction == null
            ? await db.Database.BeginTransactionAsync(cancellationToken) : null;
        var before = id == 0 ? null : await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        var result = await next();
        if (result is not Ticket ticket) return result; // Disposing an owned transaction rolls back rejected mutations.
        bool created = request is AddTicketQuery;
        bool routed = before != null && before.CurrentRoleId != ticket.CurrentRoleId;
        bool assigned = before != null && before.DeveloperId != ticket.DeveloperId;
        bool statusChanged = before != null && before.StatusId != ticket.StatusId;
        if (created || routed || assigned || statusChanged)
        {
            var type = created ? "ticket-created" : routed || assigned ? "ticket-assigned" : "ticket-status";
            var title = created ? "تیکت جدید ثبت شد" : routed ? "تیکت ارجاع شد" : assigned ? "انجام‌دهنده تیکت تغییر کرد" : "وضعیت تیکت تغییر کرد";
            var status = await db.Statuses.Where(s => s.Id == ticket.StatusId).Select(s => s.Name).FirstOrDefaultAsync(cancellationToken) ?? "نامشخص";
            var message = $"تیکت شماره {ticket.TicketNumber}: {ticket.Title}";
            if (statusChanged || created) message += $" — وضعیت: {status}";
            var developerName = DeveloperCatalog.Active.FirstOrDefault(d => d.Id == (int)ticket.DeveloperId)?.Name;
            if (assigned && developerName != null) message += $" — انجام‌دهنده: {developerName}";
            if (routed) message += ticket.CurrentRoleId == 5 ? " — ارسال به ویرا" : " — ارسال به مدیر فنی";
            if (message.Length > 1000) message = message[..997] + "…";
            var users = await db.Users.AsNoTracking().Where(u => u.IsActive)
                .Select(u => new { u.Id, u.Name, u.Family }).ToListAsync(cancellationToken);
            var roles = await (from ur in db.UserRoles.AsNoTracking()
                               join role in db.Roles.AsNoTracking() on ur.RoleId equals role.Id
                               where role.IsActive select new { ur.UserId, ur.RoleId }).ToListAsync(cancellationToken);
            foreach (var user in users)
            {
                var userRoles = roles.Where(r => r.UserId == user.Id).Select(r => r.RoleId).Distinct().ToArray();
                if (userRoles.Length == 0) continue;
                bool assignedDeveloper = developerName != null && Normalize($"{user.Name} {user.Family}") == Normalize(developerName);
                bool recipient = user.Id == ticket.UserId || userRoles.Any(r => r is 4 or 5 || r == ticket.CurrentRoleId) ||
                    userRoles.Contains(2) && (ticket.CurrentRoleId == 5 && (routed || created) || assigned && assignedDeveloper);
                if (!recipient || !TicketAccessPolicy.CanView(ticket, new Recipient(user.Id, userRoles))) continue;
                db.Notifications.Add(new TicketingNotification {
                    UserId = user.Id, Title = title, Message = message, Type = type,
                    TargetUrl = $"/ticket/detail?id={ticket.Id}", IsRead = false, CreatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        if (transaction != null) await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static string Normalize(string name) => string.Join(" ", name.Replace('ي', 'ی').Replace('ك', 'ک').Replace('\u200c', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));
    private sealed record Recipient(int Id, int[] Roles) : ICurrentUserService
    {
        public int? UserId => Id;
        public string? Username => null;
        public int? DefaultRoleId => Roles.FirstOrDefault();
        public IReadOnlyList<int> RoleIds => Roles;
        public bool IsAuthenticated => true;
        public bool IsAdminVira => HasRole(5);
        public bool IsAdminTaz => HasRole(4);
        public bool HasRole(int role) => Roles.Contains(role);
    }
}
