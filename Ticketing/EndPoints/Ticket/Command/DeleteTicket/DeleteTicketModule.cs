using Carter;
using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;
using Ticketing.Services;

namespace Ticketing.EndPoints.Ticket.Command.DeleteTicket;

public sealed class DeleteTicketModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/v1/tickets/{id:int}", async (
            int id, TicketingDbContext db, ICurrentUserService user,
            ILogger<DeleteTicketModule> logger, CancellationToken ct) =>
        {
            if (id <= 0) return Results.BadRequest(new { message = "شناسه تیکت معتبر نیست." });
            await using var transaction = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);
            var ticket = await db.Tickets.FromSqlInterpolated(
                $"SELECT * FROM dbo.Tickets WITH (UPDLOCK, HOLDLOCK) WHERE Id = {id}")
                .SingleOrDefaultAsync(ct);
            if (ticket == null) return Results.NotFound(new { message = "تیکت یافت نشد." });
            if (!TicketAccessPolicy.CanDelete(ticket, user)) return Results.Forbid();
            if (!user.IsAdminVira && await db.TicketFlows.AsNoTracking()
                    .AnyAsync(flow => flow.TicketId == id && flow.StatusId != 2, ct))
                return Results.Forbid();

            var attachments = await db.Massages.AsNoTracking().Where(message => message.TicketId == id)
                .Select(message => message.FilePath).ToListAsync(ct);
            attachments.Add(ticket.FilePath);
            await db.Massages.Where(message => message.TicketId == id).ExecuteDeleteAsync(ct);
            await db.TicketFlows.Where(flow => flow.TicketId == id).ExecuteDeleteAsync(ct);
            var target = $"/ticket/detail?id={id}";
            await db.Notifications.Where(notification => notification.TargetUrl == target)
                .ExecuteDeleteAsync(ct);
            db.Tickets.Remove(ticket);
            db.AuditLogs.Add(new AuditLog {
                ActorUserId = user.UserId, ActorUsername = user.Username,
                Action = "DeleteTicket", EntityName = "Ticket", EntityId = id.ToString(),
                Details = $"Deleted ticket {ticket.TicketNumber}; status {ticket.StatusId}",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            var filesRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Files"));
            foreach (var attachment in attachments.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct())
            {
                // Uploaded files use server-generated GUID names. Never delete a client-supplied path.
                if (Path.GetFileName(attachment) != attachment ||
                    !Guid.TryParse(Path.GetFileNameWithoutExtension(attachment), out _)) continue;
                try
                {
                    var path = Path.GetFullPath(Path.Combine(filesRoot, attachment));
                    if (Path.GetDirectoryName(path) == filesRoot && File.Exists(path)) File.Delete(path);
                }
                catch (Exception ex) { logger.LogWarning(ex, "Could not remove attachment for deleted ticket {TicketId}", id); }
            }
            return Results.Ok(new { isSuccess = true });
        }).RequireAuthorization().WithTags("Ticket");
    }
}
