using Ticketing.Domain.Entities;
using Ticketing.Domain.Models;

namespace Ticketing.Domain.Contracts;

public interface IAdminManagementService
{
    // User management
    Task<PagedResult<AdminUserDto>> GetUsersAsync(int pageNumber, int pageSize, string? search = null, int? roleId = null, int? projectId = null, bool? isActive = null);
    Task<AdminUserDto?> GetUserDetailAsync(int userId);
    Task<TicketingUser> CreateUserAsync(CreateUserRequest request);
    Task<bool> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task<bool> ToggleUserStatusAsync(int userId, bool isActive);
    Task<bool> ResetUserPasswordAsync(int userId, string newPassword);
    Task<UserStatsDto> GetUserStatsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserRoleItemDto>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> AddUserRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);
    Task<bool> RemoveUserRoleAsync(int userId, int roleId, CancellationToken cancellationToken = default);

    // Teams
    Task<IReadOnlyList<TeamDto>> GetTeamsAsync();
    Task<TicketingTeam> CreateTeamAsync(string name, string? description, int? leaderUserId, IEnumerable<int> memberUserIds);
    Task<bool> UpdateTeamAsync(int teamId, string name, string? description, int? leaderUserId, bool isActive, IEnumerable<int> memberUserIds);
    Task<bool> DeleteTeamAsync(int teamId);

    // Projects
    Task<IReadOnlyList<Project>> GetAllProjectsAsync();
    Task<Project> CreateProjectAsync(string name);
    Task<bool> UpdateProjectAsync(int projectId, string name);

    // SLA Policies
    Task<IReadOnlyList<SlaPolicyDto>> GetSlaPoliciesAsync();
    Task<SlaPolicy> CreateSlaPolicyAsync(SlaPolicyDto dto);
    Task<bool> UpdateSlaPolicyAsync(int id, SlaPolicyDto dto);
    Task<bool> DeleteSlaPolicyAsync(int id);

    // Settings
    Task<IReadOnlyList<AppSettingDto>> GetSettingsAsync(string? category = null);
    Task<bool> UpdateSettingsAsync(IEnumerable<AppSettingDto> settings);
}
