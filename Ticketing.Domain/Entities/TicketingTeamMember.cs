using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class TicketingTeamMember : BaseEntity<int>
{
    public int TeamId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual TicketingTeam? Team { get; set; }
    public virtual TicketingUser? User { get; set; }
}
