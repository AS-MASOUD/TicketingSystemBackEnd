using MediatR;
using Ticketing.Services;
using Ticketing.Domain.Contracts;

namespace Ticketing.EndPoints.Ticket.Query.GetGroupTicketInfo;

public class GetRoleTicketInfoHandler
{
    public class Handler(ITicketService ticketService, IProjectService projectService, IStatusService statusService, ILogger<GetRoleTicketInfoHandler> _logger, ICurrentUserService currentUser) : IRequestHandler<GetRoleTicketInfoQuery, object>
    {
        public async Task<object> Handle(GetRoleTicketInfoQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var listtickets = (await ticketService.ListAsync(TicketVisibility.For(currentUser)))
                    .Where(t => request.RequestTypeId <= 0 || (int?)t.RequestTypeId == request.RequestTypeId).ToList();
                var inProgresCount = currentUser.IsAdminVira || currentUser.IsAdminTaz
                    ? listtickets.Count(t => t.StatusId == 8)
                    : listtickets.Count(t => t.StatusId is not (1 or 2 or 4));

                return new
                {
                    done = listtickets.Where(a => a.StatusId == 1).Count(),
                    inserted = listtickets.Where(a => a.StatusId == 2).Count(),
                    sendtovira = listtickets.Where(a => a.StatusId == 3).Count(),
                    rejected = listtickets.Where(a => a.StatusId == 4).Count(),
                    sendtotaz = listtickets.Where(a => a.StatusId == 5).Count(),
                    awaitingConfirmation = listtickets.Where(a => a.StatusId == 6).Count(),
                    inLine = listtickets.Where(a => a.StatusId == 7).Count(),
                    inProgress = inProgresCount,
                    awaitingRejecting = listtickets.Where(a => a.StatusId == 9).Count(),
                    total = listtickets.Count()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Get Error : GetGroupTicketInfoQuery- Handle " + ex.Message);
            }

            return null;
        }
    }
}