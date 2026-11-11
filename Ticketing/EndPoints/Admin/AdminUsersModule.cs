using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Models;
using Ticketing.Services;

namespace Ticketing.EndPoints.Admin;

public class AdminUsersModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/users")
            .WithTags("Admin Users")
            .RequireAuthorization();

        group.AddEndpointFilter(async (context, next) => {
            try { return await next(context); }
            catch (Ticketing.Application.Service.Admin.AdminValidationException ex) {
                return Results.BadRequest(new { isSuccess = false, message = ex.Message });
            }
        });

        // 1. Get users paged & filtered
        group.MapGet("", async (
            int? page,
            int? pageSize,
            string? search,
            int? roleId,
            int? projectId,
            bool? isActive,
            IAdminManagementService adminService,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.view"))
                return Results.Forbid();

            int p = page.GetValueOrDefault(1);
            int ps = pageSize.GetValueOrDefault(10);
            var result = await adminService.GetUsersAsync(p, ps, search, roleId, projectId, isActive);
            return Results.Ok(new { isSuccess = true, data = result });
        });

        // 2. Get user by ID
        group.MapGet("/{id:int}", async (
            int id,
            IAdminManagementService adminService,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.view"))
                return Results.Forbid();

            var user = await adminService.GetUserDetailAsync(id);
            if (user == null)
                return Results.NotFound(new { isSuccess = false, message = "کاربر مورد نظر یافت نشد." });

            return Results.Ok(new { isSuccess = true, data = user });
        });

        // 3. Create user
        group.MapPost("", async (
            CreateUserRequest request,
            IAdminManagementService adminService,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.create"))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { isSuccess = false, message = "نام کاربری و کلمه عبور الزامی هستند." });

            try
            {
                var createdUser = await adminService.CreateUserAsync(request);
                await auditService.LogAsync("CreateUser", "TicketingUser", createdUser.Id.ToString(), $"ایجاد کاربر {createdUser.Username}", currentUser.UserId, currentUser.Username);
                return Results.Ok(new { isSuccess = true, message = "کاربر با موفقیت ایجاد شد.", data = new { id = createdUser.Id, username = createdUser.Username } });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { isSuccess = false, message = ex.Message });
            }
        });

        // 4. Update user
        group.MapPut("/{id:int}", async (
            int id,
            UpdateUserRequest request,
            IAdminManagementService adminService,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.update"))
                return Results.Forbid();

            bool updated = await adminService.UpdateUserAsync(id, request);
            if (!updated)
                return Results.NotFound(new { isSuccess = false, message = "کاربر یافت نشد." });

            await auditService.LogAsync("UpdateUser", "TicketingUser", id.ToString(), $"ویرایش مشخصات کاربر شناسه {id}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "اطلاعات کاربر با موفقیت به‌روزرسانی شد." });
        });

        // 5. Toggle active status
        group.MapPatch("/{id:int}/status", async (
            int id,
            bool isActive,
            IAdminManagementService adminService,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.deactivate"))
                return Results.Forbid();

            bool toggled = await adminService.ToggleUserStatusAsync(id, isActive);
            if (!toggled)
                return Results.NotFound(new { isSuccess = false, message = "کاربر یافت نشد." });

            string actionName = isActive ? "ActivateUser" : "DeactivateUser";
            await auditService.LogAsync(actionName, "TicketingUser", id.ToString(), $"تغییر وضعیت کاربر شناسه {id} به {(isActive ? "فعال" : "غیرفعال")}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = $"وضعیت کاربر به {(isActive ? "فعال" : "غیرفعال")} تغییر یافت." });
        });

        // 6. Reset password
        group.MapPost("/{id:int}/password-reset", async (
            int id,
            string newPassword,
            IAdminManagementService adminService,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.reset_password"))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return Results.BadRequest(new { isSuccess = false, message = "کلمه عبور جدید باید حداقل ۶ کاراکتر باشد." });

            bool reset = await adminService.ResetUserPasswordAsync(id, newPassword);
            if (!reset)
                return Results.NotFound(new { isSuccess = false, message = "کاربر یافت نشد." });

            await auditService.LogAsync("ResetPassword", "TicketingUser", id.ToString(), $"بازنشانی کلمه عبور کاربر شناسه {id} توسط ادمین", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "کلمه عبور کاربر با موفقیت بازنشانی شد." });
        });

        // 7. Total users statistics
        var totalUsersHandler = async (
            IAdminManagementService adminService,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.view"))
                return Results.Forbid();

            var stats = await adminService.GetUserStatsAsync();
            return Results.Ok(new { isSuccess = true, data = stats });
        };
        group.MapGet("/total", totalUsersHandler);
        group.MapGet("/stats", totalUsersHandler);

        // 8. Get roles of a user
        group.MapGet("/{id:int}/roles", async (
            int id,
            IAdminManagementService adminService,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.view"))
                return Results.Forbid();

            var roles = await adminService.GetUserRolesAsync(id);
            return Results.Ok(new { isSuccess = true, data = roles });
        });

        // 9. Add a role to a user
        group.MapPost("/{id:int}/roles", async (
            int id,
            AddUserRoleRequest request,
            IAdminManagementService adminService,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.update"))
                return Results.Forbid();

            bool added = await adminService.AddUserRoleAsync(id, request.RoleId);
            if (!added)
                return Results.BadRequest(new { isSuccess = false, message = "نقش انتخابی معتبر نیست یا کاربر یافت نشد." });

            await auditService.LogAsync("AddUserRole", "TicketingUserRole", id.ToString(), $"افزودن نقش {request.RoleId} به کاربر {id}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "نقش با موفقیت به کاربر اضافه شد." });
        });

        // 10. Remove a role from a user
        group.MapDelete("/{id:int}/roles/{roleId:int}", async (
            int id,
            int roleId,
            IAdminManagementService adminService,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "users.update"))
                return Results.Forbid();

            bool removed = await adminService.RemoveUserRoleAsync(id, roleId);
            if (!removed)
                return Results.BadRequest(new { isSuccess = false, message = "امکان حذف نقش پیش‌فرض اصلی وجود ندارد یا نقش در لیست کاربر نیست." });

            await auditService.LogAsync("RemoveUserRole", "TicketingUserRole", id.ToString(), $"حذف نقش {roleId} از کاربر {id}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "نقش با موفقیت از کاربر حذف شد." });
        });
    }
}
