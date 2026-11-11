using MediatR;

namespace Ticketing.EndPoints.Ticket.Query.GetGroupTicketList;

public class GetRoleTicketListQuery : IRequest<object?>
{
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.RoleId)]
    public int? RoleId { get; set; }
    public int? Status { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.UserId)]
    public int? UserId { get; set; }
    public int? RequestTypeId { get; set; }
}
