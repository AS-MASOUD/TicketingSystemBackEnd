using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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
        group.MapGet("/", async (
            IPermissionService permService,
            ICurrentUserService currentUser) =>
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
                PermissionCodes = r.RolePermissions.Select(rp => rp.Permission!.Code).ToList(),
                CreatedAt = r.CreatedAt
            }).ToList();

            return Results.Ok(new { isSuccess = true, data = dtos });
        });

        // 2. Get role by ID
        group.MapGet("/{id:int}", async (
            int id,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
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
                PermissionCodes = r.RolePermissions.Select(rp => rp.Permission!.Code).ToList(),
                CreatedAt = r.CreatedAt
            };

            return Results.Ok(new { isSuccess = true, data = dto });
        });

        // 3. Create role
        group.MapPost("/", async (
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
        app.MapGet("/api/v1/admin/permissions", async (
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
        }).RequireAuthorization();
    }
}
