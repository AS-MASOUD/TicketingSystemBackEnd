using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ticketing.Domain.Models;
using Ticketing.Domain.Contracts;
using Ticketing.Services;

namespace Ticketing.EndPoints.Admin;

public class AdminAuditModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/audit-logs")
            .WithTags("Admin Audit Logs")
            .RequireAuthorization();

        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? action,
            string? actorUsername,
            DateTime? fromDate,
            DateTime? toDate,
            IAuditService auditService,
            IPermissionService permService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null || !await permService.HasPermissionAsync(currentUser.UserId.Value, "audit.view"))
                return Results.Forbid();

            int p = page.GetValueOrDefault(1);
            int ps = pageSize.GetValueOrDefault(20);

            var (items, totalCount) = await auditService.GetLogsAsync(p, ps, action, actorUsername, fromDate, toDate);

            var dtos = items.Select(l => new AuditLogDto
            {
                Id = l.Id,
                ActorUserId = l.ActorUserId,
                ActorUsername = l.ActorUsername,
                Action = l.Action,
                EntityName = l.EntityName,
                EntityId = l.EntityId,
                Details = l.Details,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            }).ToList();

            return Results.Ok(new
            {
                isSuccess = true,
                data = new
                {
                    items = dtos,
                    totalCount = totalCount,
                    pageNumber = p,
                    pageSize = ps,
                    totalPages = (int)Math.Ceiling((double)totalCount / ps)
                }
            });
        });
    }
}
