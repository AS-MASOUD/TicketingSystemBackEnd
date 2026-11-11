using MediatR;

namespace Ticketing.EndPoints.Ticket.Command.ChangeStatus;

public class ChangeStatusQuery:IRequest<object>
{
    public int TicketId { get; set; }
    public int Status { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.UserId)]
    public int UserId { get; set; }
}