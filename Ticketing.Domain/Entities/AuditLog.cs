using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class AuditLog : BaseEntity<int>
{
    public int? ActorUserId { get; set; }
    public string? ActorUsername { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
