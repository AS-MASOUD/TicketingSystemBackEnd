using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class TicketingTeam : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LeaderUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TicketingTeamMember> Members { get; set; } = new List<TicketingTeamMember>();
}
