using Ticketing.Domain.Common;
using Ticketing.Domain.Entities;

namespace Ticketing.Domain.Contracts;

public interface IUserService : IAsyncRepository<TicketingUser>
{
    Task<TicketingUser?> GetByUsernameWithRolesAsync(string username);
    Task<TicketingUser?> GetByIdWithRolesAsync(int id);
    Task<bool> VerifyPasswordAsync(TicketingUser user, string password);
    Task SetPasswordAsync(TicketingUser user, string newPassword);
    Task<TicketingUser> CreateUserAsync(string username, string password, string? name, string? family, string? mobile, int defaultRoleId, IEnumerable<int> roleIds, int? explicitId = null);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<IReadOnlyList<TicketingUserRole>> GetUserRolesAsync(int userId);
    Task EnsureSeedUsersAsync();
}
