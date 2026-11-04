using System.Security.Claims;

namespace Ticketing.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public int? UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out int id))
                return id;
            return null;
        }
    }

    public string? Username => User?.Identity?.Name;

    public int? DefaultRoleId
    {
        get
        {
            var claim = User?.FindFirst("DefaultRole")?.Value;
            if (int.TryParse(claim, out int r))
                return r;
            return RoleIds.FirstOrDefault();
        }
    }

    public IReadOnlyList<int> RoleIds
    {
        get
        {
            if (User == null)
                return Array.Empty<int>();

            return User.FindAll(ClaimTypes.Role)
                .Select(c => int.TryParse(c.Value, out int r) ? r : (int?)null)
                .Where(r => r.HasValue)
                .Select(r => r!.Value)
                .Distinct()
                .ToList();
        }
    }

    public bool IsAdminVira => HasRole(5);

    public bool IsAdminTaz => HasRole(4);

    public bool HasRole(int roleId) => RoleIds.Contains(roleId);
}
