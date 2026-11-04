using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class TicketingUser : BaseEntity<int>
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Family { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public bool IsActive { get; set; } = true;
    public int DefaultRoleId { get; set; } = 2;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<TicketingUserRole> UserRoles { get; set; } = new List<TicketingUserRole>();
    public virtual ICollection<UserProject> UserProjects { get; set; } = new List<UserProject>();
}
