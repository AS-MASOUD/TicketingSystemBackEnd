using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class TicketingRole : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TicketingRolePermission> RolePermissions { get; set; } = new List<TicketingRolePermission>();
}
