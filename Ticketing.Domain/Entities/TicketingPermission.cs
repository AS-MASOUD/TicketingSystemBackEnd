using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class TicketingPermission : BaseEntity<int>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TicketingRolePermission> RolePermissions { get; set; } = new List<TicketingRolePermission>();
}
