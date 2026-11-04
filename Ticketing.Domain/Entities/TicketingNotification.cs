using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class TicketingNotification : BaseEntity<int>
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, warning, success, error
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual TicketingUser? User { get; set; }
}
