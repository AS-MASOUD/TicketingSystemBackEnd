namespace Ticketing.Domain.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
}

public class AdminUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Family { get; set; }
    public string FullName => $"{Name} {Family}".Trim();
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public bool IsActive { get; set; }
    public int DefaultRoleId { get; set; }
    public string DefaultRoleName { get; set; } = string.Empty;
    public List<int> RoleIds { get; set; } = new();
    public List<string> RoleNames { get; set; } = new();
    public List<int> ProjectIds { get; set; } = new();
    public List<string> ProjectNames { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Family { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public int DefaultRoleId { get; set; } = 2;
    public List<int> RoleIds { get; set; } = new();
    public List<int> ProjectIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class UpdateUserRequest
{
    public string? Name { get; set; }
    public string? Family { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public int DefaultRoleId { get; set; }
    public List<int> RoleIds { get; set; } = new();
    public List<int> ProjectIds { get; set; } = new();
    public bool IsActive { get; set; }
}

public class AdminRoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public int UserCount { get; set; }
    public List<string> PermissionCodes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class RoleCreateUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> PermissionCodes { get; set; } = new();
}

public class PermissionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AdminOverviewDto
{
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int OverdueTickets { get; set; }
    public int UnassignedTickets { get; set; }
    public double AverageResolutionHours { get; set; }
    public double AverageFirstResponseHours { get; set; }
    public double SlaComplianceRate { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveProjects { get; set; }
}

public class TicketTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ClosedCount { get; set; }
}

public class StatusDistributionDto
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class PriorityDistributionDto
{
    public int PriorityId { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class SlaPerformanceDto
{
    public int WithinSlaCount { get; set; }
    public int ApproachingBreachCount { get; set; }
    public int BreachedCount { get; set; }
    public double ComplianceRate { get; set; }
}

public class ProjectWorkloadDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int OverdueTickets { get; set; }
}

public class TeamPerformanceDto
{
    public int DeveloperId { get; set; }
    public string DeveloperName { get; set; } = string.Empty;
    public int AssignedCount { get; set; }
    public int ResolvedCount { get; set; }
    public double AverageResolutionHours { get; set; }
}

public class TeamDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LeaderUserId { get; set; }
    public string? LeaderName { get; set; }
    public bool IsActive { get; set; }
    public int MemberCount { get; set; }
    public List<int> MemberUserIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class SlaPolicyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public int ResponseTimeHours { get; set; }
    public int ResolutionTimeHours { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public bool IsActive { get; set; }
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info";
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogDto
{
    public int Id { get; set; }
    public int? ActorUserId { get; set; }
    public string? ActorUsername { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AppSettingDto
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}
