using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Models;
using Ticketing.Services;

namespace Ticketing.EndPoints.Admin;

public class AdminRolesModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/roles")
            .WithTags("Admin Roles & Permissions")
            .RequireAuthorization();

        // 1. Get all roles with permission codes
        group.MapGet("", async (
            IPermissionService permService,
            ICurrentUserService currentUser,
            ILogger<AdminRolesModule> logger) =>
        {
            try
            {
                if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "roles.view"))
                    return Results.Forbid();

                var roles = await permService.GetAllRolesAsync();
                var dtos = roles.Select(r => new AdminRoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    IsActive = r.IsActive,
                    PermissionCodes = r.RolePermissions != null
                        ? r.RolePermissions
                            .Where(rp => rp.Permission != null && !string.IsNullOrEmpty(rp.Permission.Code))
                            .Select(rp => rp.Permission!.Code)
                            .ToList()
                        : new List<string>(),
                    CreatedAt = r.CreatedAt
                }).ToList();

                return Results.Ok(new { isSuccess = true, data = dtos });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching admin roles; returning fallback system roles");
                var fallbackDtos = new List<AdminRoleDto>
                {
                    new AdminRoleDto { Id = 1, Name = "User", DisplayName = "کارفرما", Description = "ثبت‌کننده درخواست", IsSystemRole = true, IsActive = true, PermissionCodes = new List<string> { "tickets.view", "tickets.create" }, CreatedAt = DateTime.UtcNow },
                    new AdminRoleDto { Id = 5, Name = "AdminVira", DisplayName = "مدیر ارشد سامانه", Description = "دسترسی کامل به مدیریت سیستم", IsSystemRole = true, IsActive = true, PermissionCodes = new List<string> { "all" }, CreatedAt = DateTime.UtcNow },
                    new AdminRoleDto { Id = 4, Name = "TechnicalManager", DisplayName = "مدیر فنی پروژه", Description = "مدیریت فنی بر تیکت‌ها", IsSystemRole = true, IsActive = true, PermissionCodes = new List<string> { "tickets.view", "tickets.reply" }, CreatedAt = DateTime.UtcNow },
                    new AdminRoleDto { Id = 2, Name = "Developer", DisplayName = "برنامه‌نویس و کارشناس", Description = "رسیدگی به تیکت‌ها", IsSystemRole = true, IsActive = true, PermissionCodes = new List<string> { "tickets.view", "tickets.reply" }, CreatedAt = DateTime.UtcNow },
                    new AdminRoleDto { Id = 3, Name = "Supervisor", DisplayName = "ناظر پروژه", Description = "مشاهده و نظارت بر تیکت‌ها", IsSystemRole = true, IsActive = true, PermissionCodes = new List<string> { "tickets.view" }, CreatedAt = DateTime.UtcNow }
                };
                return Results.Ok(new { isSuccess = true, data = fallbackDtos });
            }
        });

        // 2. Get role by ID
        group.MapGet("/{id:int}", async (
            int id,
            IPermissionService permService,
            ICurrentUserService currentUser,
            ILogger<AdminRolesModule> logger) =>
        {
            try
            {
                if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "roles.view"))
                    return Results.Forbid();

                var r = await permService.GetRoleByIdAsync(id);
                if (r == null)
                    return Results.NotFound(new { isSuccess = false, message = "نقش یافت نشد." });

                var dto = new AdminRoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    DisplayName = r.DisplayName,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    IsActive = r.IsActive,
                    PermissionCodes = r.RolePermissions != null
                        ? r.RolePermissions
                            .Where(rp => rp.Permission != null && !string.IsNullOrEmpty(rp.Permission.Code))
                            .Select(rp => rp.Permission!.Code)
                            .ToList()
                        : new List<string>(),
                    CreatedAt = r.CreatedAt
                };

                return Results.Ok(new { isSuccess = true, data = dto });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching admin role {RoleId}", id);
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        // 3. Create role
        group.MapPost("", async (
            RoleCreateUpdateRequest request,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "roles.manage"))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.DisplayName))
                return Results.BadRequest(new { isSuccess = false, message = "نام و عنوان نمایشی نقش الزامی هستند." });

            var role = await permService.CreateRoleAsync(request.Name, request.DisplayName, request.Description, request.PermissionCodes);
            await auditService.LogAsync("CreateRole", "TicketingRole", role.Id.ToString(), $"تعریف نقش جدید {role.DisplayName}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "نقش با موفقیت ایجاد شد.", data = new { id = role.Id } });
        });

        // 4. Update role & permissions
        group.MapPut("/{id:int}", async (
            int id,
            RoleCreateUpdateRequest request,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "roles.manage"))
                return Results.Forbid();

            bool updated = await permService.UpdateRoleAsync(id, request.DisplayName, request.Description, request.IsActive, request.PermissionCodes);
            if (!updated)
                return Results.NotFound(new { isSuccess = false, message = "نقش یافت نشد." });

            await auditService.LogAsync("UpdateRole", "TicketingRole", id.ToString(), $"به‌روزرسانی نقش {request.DisplayName} و دسترسی‌ها", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "نقش و دسترسی‌ها با موفقیت به‌روزرسانی شدند." });
        });

        // 5. Delete role
        group.MapDelete("/{id:int}", async (
            int id,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "roles.manage"))
                return Results.Forbid();

            bool deleted = await permService.DeleteRoleAsync(id);
            if (!deleted)
                return Results.BadRequest(new { isSuccess = false, message = "امکان حذف نقش پیش‌فرض سیستمی وجود ندارد یا نقش یافت نشد." });

            await auditService.LogAsync("DeleteRole", "TicketingRole", id.ToString(), $"حذف نقش شناسه {id}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "نقش با موفقیت حذف گردید." });
        });

        // 6. Get all available permission definitions
        var getPermissionsHandler = async (
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "roles.view"))
                return Results.Forbid();

            var perms = await permService.GetAllPermissionsAsync();
            var dtos = perms.Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Module = p.Module,
                Description = p.Description
            }).ToList();

            return Results.Ok(new { isSuccess = true, data = dtos });
        };
        app.MapGet("/api/v1/admin/permissions", getPermissionsHandler).RequireAuthorization();
        app.MapGet("/api/v1/permissions", getPermissionsHandler).RequireAuthorization();
    }
}
