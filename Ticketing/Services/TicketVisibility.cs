using System.Linq.Expressions;
using Ticketing.Domain.Entities;

namespace Ticketing.Services;

/// <summary>Apply the same visibility rules in SQL for lists, counters, and search.</summary>
public static class TicketVisibility
{
    public static Expression<Func<Ticket, bool>> For(ICurrentUserService user)
    {
        if (!user.IsAuthenticated || user.UserId is not > 0) return t => false;
        if (user.IsAdminVira || user.IsAdminTaz || user.HasRole(3)) return t => true;
        int id = user.UserId.Value;
        bool developer = user.HasRole(2);
        int[] roles = user.RoleIds.Where(r => r > 5).ToArray();
        return t => t.UserId == id ||
            developer && (t.CurrentRoleId == 2 || t.CurrentRoleId == 5) ||
            roles.Contains(t.CurrentRoleId);
    }
}
