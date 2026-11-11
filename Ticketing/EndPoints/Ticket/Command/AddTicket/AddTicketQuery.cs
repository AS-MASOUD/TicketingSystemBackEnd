using MediatR;
using Ticketing.Domain.Enums;

namespace Ticketing.EndPoints.Ticket.Command.AddTicket;

public class AddTicketQuery:IRequest<object>
{
    public string Title { get; set; }
    public string Text { get; set; }
    public Priority Priority { get; set; }
    public RequestType RequestType { get; set; }
    public int ProjectId { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.RoleId)]
    public int RoleId { get; set; }
    public IFormFile? File { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.UserId)]
    public int UserId { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.Username)]
    public string Username { get; set; }
    public IsSchedule? IsSchedule { get; set; }
}