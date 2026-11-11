using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Application.Service.Admin;

public class PermissionService : IPermissionService
{
    private readonly TicketingDbContext _dbContext;
    private readonly ILogger<PermissionService>? _logger;

    public PermissionService(TicketingDbContext dbContext, ILogger<PermissionService>? logger = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(int userId, string permissionCode)
    {
        try
        {
            var roleIds = await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (!roleIds.Any())
                return false;

            // If user has role 5 (AdminVira / System Admin), grant all permissions
            if (roleIds.Contains(5))
                return true;

            if (string.IsNullOrWhiteSpace(permissionCode))
                return false;

            var normalizedCode = permissionCode.Trim().ToLowerInvariant();

            var userPerms = await _dbContext.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission!.Code.ToLower())
                .ToListAsync();

            // Support legacy punctuation/casing without turning view grants into manage grants.
            return userPerms.Any(p => p.Replace(".", "") == normalizedCode.Replace(".", "") ||
                p == "users.manage" && new[] { "users.view", "users.create", "users.edit", "users.resetpassword", "users.togglestatus", "users.update", "users.deactivate", "users.reset_password" }.Contains(normalizedCode) ||
                normalizedCode.EndsWith(".view") && p == normalizedCode[..^5] + ".manage");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed in permission check for user {UserId}", userId);
            return false; // Fail-secure: deny access on lookup failure
        }
    }

    public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(int userId)
    {
        try
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
                try
                {
                    return await _dbContext.Permissions
                        .Select(p => p.Code)
                        .Distinct()
                        .ToListAsync();
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }

            return await _dbContext.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission!.Code)
                .Distinct()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get user permissions for user {UserId}", userId);
            return Array.Empty<string>();
        }
    }

    public async Task<IReadOnlyList<TicketingRole>> GetAllRolesAsync()
    {
        try
        {
            return await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .OrderBy(r => r.Id)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load roles with permissions join; attempting direct roles query");
            try
            {
                return await _dbContext.Roles
                    .OrderBy(r => r.Id)
                    .ToListAsync();
            }
            catch (Exception ex2)
            {
                _logger?.LogError(ex2, "Failed to query TicketingRoles from database, returning default system roles");
                throw;
            }
        }
    }

    public async Task<TicketingRole?> GetRoleByIdAsync(int id)
    {
        try
        {
            return await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to load role {RoleId} with permissions; attempting direct role query", id);
            try
            {
                return await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id);
            }
            catch
            {
                return GetDefaultSystemRoles().FirstOrDefault(r => r.Id == id);
            }
        }
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
            try
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
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to assign permissions to new role {RoleId}", role.Id);
            }
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
        if (role.RolePermissions != null)
        {
            _dbContext.RolePermissions.RemoveRange(role.RolePermissions);
        }

        if (permissionCodes != null && permissionCodes.Any())
        {
            try
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
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to update permissions for role {RoleId}", id);
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
        try
        {
            return await _dbContext.Permissions
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query permissions from database, returning default permission list");
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> GetRolePermissionCodesAsync(int roleId)
    {
        try
        {
            return await _dbContext.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission!.Code)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get role permission codes for role {RoleId}", roleId);
            return Array.Empty<string>();
        }
    }

    private static IReadOnlyList<TicketingRole> GetDefaultSystemRoles()
    {
        return new List<TicketingRole>
        {
            new TicketingRole { Id = 1, Name = "User", DisplayName = "کارفرما / کارشناس", Description = "کارفرما، کارشناس و ثبت‌کننده تیکت", IsSystemRole = true, IsActive = true, CreatedAt = DateTime.UtcNow },
            new TicketingRole { Id = 5, Name = "AdminVira", DisplayName = "مدیر ارشد سامانه", Description = "دسترسی کامل به مدیریت سیستم", IsSystemRole = true, IsActive = true, CreatedAt = DateTime.UtcNow },
            new TicketingRole { Id = 4, Name = "TechnicalManager", DisplayName = "مدیر فنی پروژه", Description = "مدیریت و نظارت فنی بر تیکت‌ها", IsSystemRole = true, IsActive = true, CreatedAt = DateTime.UtcNow },
            new TicketingRole { Id = 2, Name = "Developer", DisplayName = "برنامه‌نویس و کارشناس", Description = "رسیدگی و پاسخ‌دهی به تیکت‌ها", IsSystemRole = true, IsActive = true, CreatedAt = DateTime.UtcNow },
            new TicketingRole { Id = 3, Name = "Supervisor", DisplayName = "ناظر پروژه", Description = "مشاهده و نظارت بر جریان تیکت‌ها", IsSystemRole = true, IsActive = true, CreatedAt = DateTime.UtcNow }
        };
    }

    private static IReadOnlyList<TicketingPermission> GetDefaultPermissions()
    {
        return new List<TicketingPermission>
        {
            new TicketingPermission { Id = 1, Code = "tickets.view", Name = "مشاهده تیکت‌ها", Module = "Tickets", Description = "مشاهده لیست و جزئیات تیکت‌ها" },
            new TicketingPermission { Id = 2, Code = "tickets.create", Name = "ثبت تیکت جدید", Module = "Tickets", Description = "امکان ایجاد و ارسال تیکت" },
            new TicketingPermission { Id = 3, Code = "tickets.reply", Name = "پاسخ‌دهی به تیکت", Module = "Tickets", Description = "ثبت پیام و پاسخ به تیکت‌ها" },
            new TicketingPermission { Id = 4, Code = "tickets.change_status", Name = "تغییر وضعیت تیکت", Module = "Tickets", Description = "تغییر گردش کار و وضعیت تیکت" },
            new TicketingPermission { Id = 5, Code = "users.view", Name = "مشاهده کاربران", Module = "Users", Description = "مشاهده لیست کاربران سامانه" },
            new TicketingPermission { Id = 6, Code = "users.manage", Name = "مدیریت کاربران", Module = "Users", Description = "ایجاد، ویرایش و مدیریت کاربران" },
            new TicketingPermission { Id = 7, Code = "roles.view", Name = "مشاهده نقش‌ها", Module = "Roles", Description = "مشاهده لیست نقش‌ها و دسترسی‌ها" },
            new TicketingPermission { Id = 8, Code = "roles.manage", Name = "مدیریت نقش‌ها", Module = "Roles", Description = "ایجاد و تخصیص نقش‌ها و دسترسی‌ها" },
            new TicketingPermission { Id = 9, Code = "reports.view", Name = "مشاهده گزارش‌ها", Module = "Reports", Description = "دسترسی به گزارش‌ها و نمودارهای تحلیلی" },
            new TicketingPermission { Id = 10, Code = "audit.view", Name = "مشاهده ردپای ممیزی", Module = "Audit", Description = "مشاهده لاگ‌های امنیتی و رویدادها" }
        };
    }
}
