using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Models;
using Ticketing.Services;

namespace Ticketing.EndPoints.Admin;

public class AdminManagementModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // ------------------ TEAMS ------------------
        var teamGroup = app.MapGroup("/api/v1/admin/teams")
            .WithTags("Admin Teams")
            .RequireAuthorization();

        teamGroup.MapGet("", async (
            IAdminManagementService service,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "teams.view"))
                return Results.Forbid();

            var teams = await service.GetTeamsAsync();
            return Results.Ok(new { isSuccess = true, data = teams });
        });

        teamGroup.MapPost("", async (
            TeamDto dto,
            IAdminManagementService service,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "teams.manage"))
                return Results.Forbid();

            var team = await service.CreateTeamAsync(dto.Name, dto.Description, dto.LeaderUserId, dto.MemberUserIds);
            await auditService.LogAsync("CreateTeam", "TicketingTeam", team.Id.ToString(), $"ایجاد تیم کاری {team.Name}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "تیم با موفقیت ایجاد شد.", data = new { id = team.Id } });
        });

        teamGroup.MapPut("/{id:int}", async (
            int id,
            TeamDto dto,
            IAdminManagementService service,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "teams.manage"))
                return Results.Forbid();

            bool updated = await service.UpdateTeamAsync(id, dto.Name, dto.Description, dto.LeaderUserId, dto.IsActive, dto.MemberUserIds);
            if (!updated)
                return Results.NotFound(new { isSuccess = false, message = "تیم یافت نشد." });

            await auditService.LogAsync("UpdateTeam", "TicketingTeam", id.ToString(), $"ویرایش تیم {dto.Name}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "تیم با موفقیت به‌روزرسانی شد." });
        });

        teamGroup.MapDelete("/{id:int}", async (
            int id,
            IAdminManagementService service,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "teams.manage"))
                return Results.Forbid();

            bool deleted = await service.DeleteTeamAsync(id);
            if (!deleted)
                return Results.NotFound(new { isSuccess = false, message = "تیم یافت نشد." });

            await auditService.LogAsync("DeleteTeam", "TicketingTeam", id.ToString(), $"حذف تیم شناسه {id}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "تیم حذف شد." });
        });

        // ------------------ PROJECTS ------------------
        var projectGroup = app.MapGroup("/api/v1/admin/projects")
            .WithTags("Admin Projects")
            .RequireAuthorization();

        var getProjectsHandler = async (
            IAdminManagementService service,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "projects.view"))
                return Results.Forbid();

            var projects = await service.GetAllProjectsAsync();
            return Results.Ok(new { isSuccess = true, data = projects });
        };
        projectGroup.MapGet("", getProjectsHandler);

        // Also alias /api/v1/projects for maximum frontend and proxy compatibility
        var generalProjectGroup = app.MapGroup("/api/v1/projects")
            .WithTags("Projects")
            .RequireAuthorization();
        generalProjectGroup.MapGet("", getProjectsHandler);

        projectGroup.MapPost("", async (
            string name,
            IAdminManagementService service,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "projects.manage"))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(name))
                return Results.BadRequest(new { isSuccess = false, message = "نام پروژه الزامی است." });

            var p = await service.CreateProjectAsync(name);
            await auditService.LogAsync("CreateProject", "Project", p.Id.ToString(), $"ایجاد پروژه جدید {p.Name}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "پروژه ایجاد شد.", data = p });
        });

        projectGroup.MapPut("/{id:int}", async (
            int id,
            string name,
            IAdminManagementService service,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "projects.manage"))
                return Results.Forbid();

            if (string.IsNullOrWhiteSpace(name))
                return Results.BadRequest(new { isSuccess = false, message = "نام پروژه الزامی است." });

            bool updated = await service.UpdateProjectAsync(id, name);
            if (!updated)
                return Results.NotFound(new { isSuccess = false, message = "پروژه یافت نشد." });

            await auditService.LogAsync("UpdateProject", "Project", id.ToString(), $"ویرایش نام پروژه به {name}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "پروژه به‌روزرسانی شد." });
        });

        // ------------------ SLA POLICIES ------------------
        var slaGroup = app.MapGroup("/api/v1/admin/sla")
            .WithTags("Admin SLA")
            .RequireAuthorization();

        slaGroup.MapGet("", async (
            IAdminManagementService service,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "sla.view"))
                return Results.Forbid();

            var policies = await service.GetSlaPoliciesAsync();
            return Results.Ok(new { isSuccess = true, data = policies });
        });

        slaGroup.MapPost("", async (
            SlaPolicyDto dto,
            IAdminManagementService service,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "sla.manage"))
                return Results.Forbid();

            var policy = await service.CreateSlaPolicyAsync(dto);
            await auditService.LogAsync("CreateSlaPolicy", "SlaPolicy", policy.Id.ToString(), $"تعریف قانون توافق سطح خدمت {policy.Name}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "قانون SLA ایجاد شد.", data = new { id = policy.Id } });
        });

        slaGroup.MapPut("/{id:int}", async (
            int id,
            SlaPolicyDto dto,
            IAdminManagementService service,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "sla.manage"))
                return Results.Forbid();

            bool updated = await service.UpdateSlaPolicyAsync(id, dto);
            if (!updated)
                return Results.NotFound(new { isSuccess = false, message = "قانون SLA یافت نشد." });

            await auditService.LogAsync("UpdateSlaPolicy", "SlaPolicy", id.ToString(), $"ویرایش قانون SLA {dto.Name}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "قانون SLA به‌روزرسانی شد." });
        });

        slaGroup.MapDelete("/{id:int}", async (
            int id,
            IAdminManagementService service,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "sla.manage"))
                return Results.Forbid();

            bool deleted = await service.DeleteSlaPolicyAsync(id);
            if (!deleted)
                return Results.NotFound(new { isSuccess = false, message = "قانون SLA یافت نشد." });

            await auditService.LogAsync("DeleteSlaPolicy", "SlaPolicy", id.ToString(), $"حذف قانون SLA شناسه {id}", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "قانون SLA حذف گردید." });
        });

        // ------------------ SETTINGS ------------------
        var settingGroup = app.MapGroup("/api/v1/admin/settings")
            .WithTags("Admin Settings")
            .RequireAuthorization();

        settingGroup.MapGet("", async (
            string? category,
            IAdminManagementService service,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "settings.view"))
                return Results.Forbid();

            var settings = await service.GetSettingsAsync(category);
            return Results.Ok(new { isSuccess = true, data = settings });
        });

        settingGroup.MapPut("", async (
            List<AppSettingDto> settings,
            IAdminManagementService service,
            IPermissionService permService,
            IAuditService auditService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "settings.manage"))
                return Results.Forbid();

            bool updated = await service.UpdateSettingsAsync(settings);
            await auditService.LogAsync("UpdateSettings", "AppSettings", null, $"به‌روزرسانی تنظیمات سیستمی ({settings.Count} آیتم)", currentUser.UserId, currentUser.Username);
            return Results.Ok(new { isSuccess = true, message = "تنظیمات سامانه با موفقیت ذخیره شدند." });
        });

        // ------------------ LOOKUPS ------------------
        var lookupGroup = app.MapGroup("/api/v1/admin/lookups")
            .WithTags("Admin Lookups")
            .RequireAuthorization();

        lookupGroup.MapGet("/statuses", async (
            IStatusService statusService) =>
        {
            var statuses = await statusService.ListAsync(null);
            return Results.Ok(new { isSuccess = true, data = statuses });
        });
    }
}
