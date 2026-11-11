using MediatR;
using Microsoft.IdentityModel.Tokens;
using Ticketing.Domain.Contracts;
using Ticketing.Services;

namespace Ticketing.EndPoints.Ticket.Command.EditeTicket;

public class EditeTicketHandler
{
    public class Handler(
        ITicketService ticketService,
        ICurrentUserService currentUser,
        IProjectService projectService,
        ILogger<EditeTicketHandler> _logger)
        : IRequestHandler<EditeTicketQuery, object?>
    {
        public async Task<object?> Handle(EditeTicketQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var findticket = await ticketService.GetAsync(a => a.Id == request.TicketId);
                if (findticket == null)
                    return Results.NotFound(new { message = "تیکت یافت نشد." });

                // Authorization check: only adminVira, adminTaz, or the ticket owner can edit
                if (!TicketAccessPolicy.CanEdit(findticket, currentUser))
                {
                    _logger.LogWarning("Unauthorized ticket edit attempt by user {UserId} for ticket {TicketId}", currentUser.UserId, request.TicketId);
                    return Results.Json(new { message = "ویرایش این تیکت مجاز نیست." }, statusCode: 403);
                }

                if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 1000 ||
                    string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 100000 ||
                    (int)request.Priority is < 1 or > 3 || (int)request.RequestType is < 1 or > 2 ||
                    await projectService.GetAsync(p => p.Id == request.ProjectId) == null)
                    return Results.BadRequest(new { message = "اطلاعات تیکت یا سامانه انتخاب‌شده معتبر نیست." });

                if (request.Title != null && request.Title != findticket.Title) { findticket.Title = request.Title; }

                if (!request.Text.IsNullOrEmpty() && request.Text != findticket.Text) { findticket.Text = request.Text; }

                if (request.Priority != null && request.Priority != findticket.Priority) { findticket.Priority = request.Priority; }

                if (request.RequestType != null && request.RequestType != findticket.RequestTypeId) { findticket.RequestTypeId = request.RequestType; }

                if (request.ProjectId != null && request.ProjectId != findticket.ProjectId) { findticket.ProjectId = request.ProjectId; }

                if (request.RequestType == Domain.Enums.RequestType.Develop)
                {
                    if (request.IsSchedule != null && request.IsSchedule != findticket.IsSchedule) { findticket.IsSchedule = request.IsSchedule; }
                }

                return await ticketService.UpdateAsync(findticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EditeTicketHandler Handle");
            }

            return null;
        }
    }
}
