using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Enums;
using Ticketing.Services;

namespace Ticketing.EndPoints.Ticket.Command.ChangeStatus;

public class ChangeStatusHandler
{
    public class Handler(
        ITicketService ticketService,
        ITicketFlowService ticketFlowService,
        ICurrentUserService currentUser,
        ILogger<ChangeStatusHandler> _logger)
        : IRequestHandler<ChangeStatusQuery, object?>
    {
        public async Task<object?> Handle(ChangeStatusQuery request, CancellationToken cancellationToken)
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

                if (request.Status == (int)StatusId.awaitingConfirmation || request.Status == (int)StatusId.awaitingRejecting)
                {
                    if (findticket.ProcessEndDateTime == null)
                    {
                        findticket.ProcessEndDateTime = DateTime.Now;
                    }
                    findticket.StatusId = request.Status;
                }
                else if (request.Status == (int)StatusId.done)
                {
                    findticket.CloseDate = DateTime.Now;
                    findticket.StatusId = request.Status;
                }
                else
                {
                    findticket.StatusId = request.Status;
                }

                await ticketFlowService.AddAsync(new Domain.Entities.TicketFlow
                {
                    CurrentRoleId = findticket.InsertedRoleId,
                    InsertDate = DateTime.Now,
                    StatusId = findticket.StatusId,
                    Username = !string.IsNullOrEmpty(currentUser.Username) ? currentUser.Username : findticket.Username,
                    UserId = request.UserId,
                    TicketId = findticket.Id,
                    PreviousRoleId = findticket.CurrentRoleId
                });

                return await ticketService.UpdateAsync(findticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChangeStatusHandler Handle");
            }

            return null;
        }
    }
}
