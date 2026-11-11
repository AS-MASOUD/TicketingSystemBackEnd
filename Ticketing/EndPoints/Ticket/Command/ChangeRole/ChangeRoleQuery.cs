using MediatR;

namespace Ticketing.EndPoints.Ticket.Command.ChangeGroup;

public class ChangeRoleQuery:IRequest<object>
{
    public int TicketId { get; set; }
    public int RoleId { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.UserId)]
    public int UserId { get; set; }
}