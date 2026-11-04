using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Services;

namespace Ticketing.EndPoints.Ticket.Command.ChangeGroup;

public class ChangeRoleHandler
{
    public class Handler(
        ITicketService ticketService,
        IProjectRoleService projectRoleService,
        ITicketFlowService ticketFlowService,
        ICurrentUserService currentUser,
        ILogger<ChangeRoleHandler> _logger)
        : IRequestHandler<ChangeRoleQuery, object?>
    {
        public async Task<object?> Handle(ChangeRoleQuery request, CancellationToken cancellationToken)
        {
            try
            {
                if (currentUser.IsAuthenticated && currentUser.UserId.HasValue)
                {
                    request.UserId = currentUser.UserId.Value;
                }

                var findticket = await ticketService.GetAsync(a => a.Id == request.TicketId);
                if (findticket == null)
                    return null;

                if (findticket.CurrentRoleId == 5 && request.RoleId == 4)
                {
                    findticket.LastChangeDatetime = DateTime.Now;
                    findticket.StatusId = 5;
                }
                else
                {
                    findticket.StatusId = 3;
                }

                await ticketFlowService.AddAsync(new Domain.Entities.TicketFlow
                {
                    CurrentRoleId = request.RoleId,
                    InsertDate = DateTime.Now,
                    StatusId = findticket.StatusId,
                    UserId = request.UserId,
                    Username = !string.IsNullOrEmpty(currentUser.Username) ? currentUser.Username : findticket.Username,
                    TicketId = findticket.Id,
                    PreviousRoleId = findticket.CurrentRoleId
                });

                findticket.CurrentRoleId = request.RoleId;

                return await ticketService.UpdateAsync(findticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChangeRoleHandler Handle");
            }
            return null;
        }
    }
}
