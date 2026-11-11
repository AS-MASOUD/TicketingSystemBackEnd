using MediatR;

namespace Ticketing.EndPoints.Ticket.Query.GetYearTickerInfo;

public class GetYearTickerInfoQuery:IRequest<object>
{
    public int? Date { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.RoleId)]
    public int RoleId { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.UserId)]
    public int UserId { get; set; }
    public int RequestTypeId { set; get; }
}