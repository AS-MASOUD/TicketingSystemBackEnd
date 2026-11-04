using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Application.Service.Admin;

public class PermissionService : IPermissionService
{
    private readonly TicketingDbContext _dbContext;

    public PermissionService(TicketingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        var roleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (!roleIds.Any())
            return false;

        // If user is AdminVira (role 5), grant all permissions
        if (roleIds.Contains(5))
            return true;

        return await _dbContext.RolePermissions
            .AnyAsync(rp => roleIds.Contains(rp.RoleId) && rp.Permission!.Code == permissionCode);
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(int userId)
    {
        var roleIds = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (!roleIds.Any())
            return Array.Empty<string>();

        if (roleIds.Contains(5))
        {
            // All permissions for AdminVira
            return await _dbContext.Permissions
                .Select(p => p.Code)
                .Distinct()
                .ToListAsync();
        }

        return await _dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TicketingRole>> GetAllRolesAsync()
    {
        return await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Id)
            .ToListAsync();
    }

    public async Task<TicketingRole?> GetRoleByIdAsync(int id)
    {
        return await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<TicketingRole> CreateRoleAsync(string name, string displayName, string? description, IEnumerable<string> permissionCodes)
    {
        var role = new TicketingRole
        {
            Name = name,
            DisplayName = displayName,
            Description = description,
            IsSystemRole = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Roles.AddAsync(role);
        await _dbContext.SaveChangesAsync();

        if (permissionCodes != null && permissionCodes.Any())
        {
            var perms = await _dbContext.Permissions
                .Where(p => permissionCodes.Contains(p.Code))
                .ToListAsync();

            foreach (var perm in perms)
            {
                _dbContext.RolePermissions.Add(new TicketingRolePermission
                {
                    RoleId = role.Id,
                    PermissionId = perm.Id
                });
            }
            await _dbContext.SaveChangesAsync();
        }

        return role;
    }

    public async Task<bool> UpdateRoleAsync(int id, string displayName, string? description, bool isActive, IEnumerable<string> permissionCodes)
    {
        var role = await _dbContext.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null)
            return false;

        role.DisplayName = displayName;
        role.Description = description;
        if (!role.IsSystemRole)
        {
            role.IsActive = isActive;
        }

        // Update permissions
        _dbContext.RolePermissions.RemoveRange(role.RolePermissions);

        if (permissionCodes != null && permissionCodes.Any())
        {
            var perms = await _dbContext.Permissions
                .Where(p => permissionCodes.Contains(p.Code))
                .ToListAsync();

            foreach (var perm in perms)
            {
                _dbContext.RolePermissions.Add(new TicketingRolePermission
                {
                    RoleId = role.Id,
                    PermissionId = perm.Id
                });
            }
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRoleAsync(int id)
    {
        var role = await _dbContext.Roles.FindAsync(id);
        if (role == null || role.IsSystemRole)
            return false; // Never delete system roles

        _dbContext.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<TicketingPermission>> GetAllPermissionsAsync()
    {
        return await _dbContext.Permissions
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetRolePermissionCodesAsync(int roleId)
    {
        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission!.Code)
            .ToListAsync();
    }
}
