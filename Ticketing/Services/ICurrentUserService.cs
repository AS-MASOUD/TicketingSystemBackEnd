namespace Ticketing.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    int? DefaultRoleId { get; }
    IReadOnlyList<int> RoleIds { get; }
    bool IsAuthenticated { get; }
    bool IsAdminVira { get; }
    bool IsAdminTaz { get; }
    bool HasRole(int roleId);
}
