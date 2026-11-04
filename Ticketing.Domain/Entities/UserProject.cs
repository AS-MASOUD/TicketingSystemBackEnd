using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class UserProject : BaseEntity<int>
{
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual TicketingUser? User { get; set; }
    public virtual Project? Project { get; set; }
}
