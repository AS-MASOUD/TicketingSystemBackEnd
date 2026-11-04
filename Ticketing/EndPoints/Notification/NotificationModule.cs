using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Models;
using Ticketing.Services;

namespace Ticketing.EndPoints.Notification;

public class NotificationModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        // 1. Get user notifications
        group.MapGet("/", async (
            bool? unreadOnly,
            int? limit,
            INotificationService notifService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            int l = limit.GetValueOrDefault(20);
            var items = await notifService.GetUserNotificationsAsync(currentUser.UserId.Value, unreadOnly.GetValueOrDefault(false), l);

            var dtos = items.Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                TargetUrl = n.TargetUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();

            return Results.Ok(new { isSuccess = true, data = dtos });
        });

        // 2. Get unread count
        group.MapGet("/unread-count", async (
            INotificationService notifService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            int count = await notifService.GetUnreadCountAsync(currentUser.UserId.Value);
            return Results.Ok(new { isSuccess = true, data = new { count } });
        });

        // 3. Mark single notification as read
        group.MapPatch("/{id:int}/read", async (
            int id,
            INotificationService notifService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            bool marked = await notifService.MarkAsReadAsync(id, currentUser.UserId.Value);
            return Results.Ok(new { isSuccess = marked });
        });

        // 4. Mark all as read
        group.MapPost("/read-all", async (
            INotificationService notifService,
            ICurrentUserService currentUser) =>
        {
            if (currentUser.UserId == null)
                return Results.Unauthorized();

            await notifService.MarkAllAsReadAsync(currentUser.UserId.Value);
            return Results.Ok(new { isSuccess = true, message = "تمامی اعلان‌ها خوانده شدند." });
        });
    }
}
