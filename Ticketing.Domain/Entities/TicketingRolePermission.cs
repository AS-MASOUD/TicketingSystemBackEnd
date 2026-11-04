using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class TicketingRolePermission : BaseEntity<int>
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public virtual TicketingRole? Role { get; set; }
    public virtual TicketingPermission? Permission { get; set; }
}
