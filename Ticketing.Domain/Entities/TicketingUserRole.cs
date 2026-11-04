using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class TicketingUserRole : BaseEntity<int>
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    public virtual TicketingUser? User { get; set; }
}
