using Ticketing.Domain.Entities;

namespace Ticketing.Domain.Contracts;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int userId, string permissionCode);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(int userId);
    Task<IReadOnlyList<TicketingRole>> GetAllRolesAsync();
    Task<TicketingRole?> GetRoleByIdAsync(int id);
    Task<TicketingRole> CreateRoleAsync(string name, string displayName, string? description, IEnumerable<string> permissionCodes);
    Task<bool> UpdateRoleAsync(int id, string displayName, string? description, bool isActive, IEnumerable<string> permissionCodes);
    Task<bool> DeleteRoleAsync(int id);
    Task<IReadOnlyList<TicketingPermission>> GetAllPermissionsAsync();
    Task<IReadOnlyList<string>> GetRolePermissionCodesAsync(int roleId);
}
