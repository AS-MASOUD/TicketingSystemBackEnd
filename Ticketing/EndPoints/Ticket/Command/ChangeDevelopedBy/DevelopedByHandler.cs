using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Services;
using Ticketing.Domain.Enums;

namespace Ticketing.EndPoints.Ticket.Command.ChangeDevelopedBy;

public class DevelopedByHandler
{
    public class Handler(
        ITicketService ticketService,
        ICurrentUserService currentUser,
        ILogger<DevelopedByQuery> _logger)
        : IRequestHandler<DevelopedByQuery, object?>
    {
        public async Task<object?> Handle(DevelopedByQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (!currentUser.IsAdminVira && !currentUser.IsAdminTaz && !currentUser.HasRole(2))
                {
                    _logger.LogWarning("Unauthorized attempt to change developer by user {UserId}", currentUser.UserId);
                    return null;
                }

                var ticketinfo = await ticketService.GetAsync(a => a.Id == request.TicketId);
                if (ticketinfo == null)
                    return null;

                if (!TicketAccessPolicy.CanAssign(ticketinfo, currentUser))
                    return Results.Json(new { message = "دسترسی به تخصیص این تیکت مجاز نیست." }, statusCode: 403);
                if (!DeveloperCatalog.IsAssignable(request.DeveloperId) ||
                    !int.TryParse(request.Time, out int minutes) || minutes < 0 || minutes > 10000000)
                    return Results.BadRequest(new { message = "برنامه‌نویس یا زمان واردشده معتبر نیست." });

                ticketinfo.TicketTime = request.Time;
                ticketinfo.DeveloperId = request.DeveloperId;

                var result = await ticketService.UpdateAsync(ticketinfo);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DevelopedByHandler Handle");
            }

            return null;
        }
    }
}
