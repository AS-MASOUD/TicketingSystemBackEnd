using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ticketing.Domain.Contracts;
using Ticketing.Services;

namespace Ticketing.EndPoints.Admin;

public class AdminAnalyticsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        RegisterAnalyticsRoutes(app.MapGroup("/api/v1/admin/analytics"));
        RegisterAnalyticsRoutes(app.MapGroup("/api/v1/analytics"));
    }

    private static void RegisterAnalyticsRoutes(RouteGroupBuilder group)
    {
        group.WithTags("Admin Analytics & KPI").RequireAuthorization(policy => policy.RequireRole("4", "5"));

        // 1. Overview KPIs
        group.MapGet("/overview", async (
            DateTime? fromDate,
            DateTime? toDate,
            int? projectId,
            IAdminAnalyticsService analyticsService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            var overview = await analyticsService.GetOverviewStatsAsync(fromDate, toDate, projectId);
            return Results.Ok(new { isSuccess = true, data = overview });
        });

        // 2. Ticket Trends
        group.MapGet("/trends", async (
            int? days,
            int? projectId,
            IAdminAnalyticsService analyticsService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            int d = days.GetValueOrDefault(30);
            var trends = await analyticsService.GetTicketTrendsAsync(d, projectId);
            return Results.Ok(new { isSuccess = true, data = trends });
        });

        // 3. Status Distribution
        group.MapGet("/status-distribution", async (
            int? projectId,
            IAdminAnalyticsService analyticsService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            var distribution = await analyticsService.GetStatusDistributionAsync(projectId);
            return Results.Ok(new { isSuccess = true, data = distribution });
        });

        // 4. Priority Distribution
        group.MapGet("/priority-distribution", async (
            int? projectId,
            IAdminAnalyticsService analyticsService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            var distribution = await analyticsService.GetPriorityDistributionAsync(projectId);
            return Results.Ok(new { isSuccess = true, data = distribution });
        });

        // 5. SLA Performance
        group.MapGet("/sla", async (
            int? projectId,
            IAdminAnalyticsService analyticsService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            var sla = await analyticsService.GetSlaPerformanceAsync(projectId);
            return Results.Ok(new { isSuccess = true, data = sla });
        });

        // 6. Project Workload
        group.MapGet("/project-workload", async (
            IAdminAnalyticsService analyticsService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            var workload = await analyticsService.GetProjectWorkloadAsync();
            return Results.Ok(new { isSuccess = true, data = workload });
        });

        // 7. Team Performance
        group.MapGet("/team-performance", async (
            IAdminAnalyticsService analyticsService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            var performance = await analyticsService.GetTeamPerformanceAsync();
            return Results.Ok(new { isSuccess = true, data = performance });
        });
    }
}
