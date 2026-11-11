using Ticketing.Domain.Entities;

namespace Ticketing.Services;

public static class TicketAccessPolicy
{
    public static bool CanView(Ticket? ticket, ICurrentUserService? user)
    {
        if (ticket == null || user == null)
            return false;

        if (!user.IsAuthenticated)
            return false;

        if (!user.UserId.HasValue || user.UserId.Value <= 0)
            return false;

        // Admins (role 4 Taz, role 5 Vira) and Supervisor (role 3) may view all tickets
        if (user.IsAdminVira || user.IsAdminTaz || user.HasRole(3) || user.HasRole(4) || user.HasRole(5))
            return true;

        // Requester/owner may view own ticket
        if (ticket.UserId == user.UserId.Value)
            return true;

        // Other roles may view tickets assigned to their current role
        if (ticket.CurrentRoleId > 5 && user.HasRole(ticket.CurrentRoleId) || ticket.CurrentRoleId == 2 && user.HasRole(2))
            return true;

        // Developer (role 2) additionally may view technical inbox (currentRoleId 5)
        if (user.HasRole(2) && ticket.CurrentRoleId == 5)
            return true;

        return false;
    }

    public static bool CanWork(Ticket ticket, ICurrentUserService user) =>
        CanView(ticket, user) && (user.IsAdminVira || user.IsAdminTaz ||
            user.HasRole(2) && ticket.CurrentRoleId is 2 or 5);

    public static bool CanChangeStatus(Ticket ticket, ICurrentUserService user, int status)
    {
        if (!CanView(ticket, user) || status is < 1 or > 9 || status == ticket.StatusId) return false;
        if (user.IsAdminVira || user.IsAdminTaz) return true;
        if (ticket.UserId == user.UserId && user.HasRole(1) &&
            (ticket.StatusId == 6 && status == 1 || ticket.StatusId == 9 && status == 4)) return true;
        if (!CanWork(ticket, user)) return false;
        return (ticket.StatusId, status) is (3, 7) or (3, 9) or (7, 8) or (7, 9) or (8, 6) or (8, 9);
    }

    public static bool CanRoute(Ticket ticket, ICurrentUserService user, int destination)
    {
        if (!CanView(ticket, user) || destination is not (4 or 5) || ticket.StatusId == 1 ||
            destination == ticket.CurrentRoleId && ticket.StatusId == (destination == 4 ? 5 : 3)) return false;
        if (user.IsAdminVira || user.IsAdminTaz) return true;
        if (destination == 4 && CanWork(ticket, user) && ticket.StatusId is 3 or 7 or 8 or 9) return true;
        return destination == 5 && ticket.UserId == user.UserId && user.HasRole(1) && ticket.StatusId is 5 or 6 or 9;
    }

    private static bool IsActingOwner(Ticket ticket, ICurrentUserService user) =>
        ticket.UserId == user.UserId && user.RoleIds.Any(r => r is 1 or 2 or 4 or 5 || r > 5);

    public static bool CanReply(Ticket ticket, ICurrentUserService user) =>
        CanView(ticket, user) && ticket.StatusId is not (1 or 4) &&
        (CanWork(ticket, user) || IsActingOwner(ticket, user) ||
         ticket.CurrentRoleId > 5 && user.HasRole(ticket.CurrentRoleId));

    public static bool CanEdit(Ticket ticket, ICurrentUserService user) =>
        CanView(ticket, user) && (user.IsAdminVira || user.IsAdminTaz ||
            ticket.StatusId is not (1 or 4) && (CanWork(ticket, user) || IsActingOwner(ticket, user)));

    public static bool CanAssign(Ticket ticket, ICurrentUserService user) =>
        CanWork(ticket, user) && ticket.StatusId is not (1 or 4);
}
