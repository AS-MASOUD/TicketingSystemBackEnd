using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Services;

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
                if (!currentUser.IsAdminVira)
                {
                    _logger.LogWarning("Unauthorized attempt to change developer by user {UserId}", currentUser.UserId);
                    return null;
                }

                var ticketinfo = await ticketService.GetAsync(a => a.Id == request.TicketId);
                if (ticketinfo == null)
                    return null;

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
