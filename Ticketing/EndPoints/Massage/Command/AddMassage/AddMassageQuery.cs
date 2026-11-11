using MediatR;

namespace Ticketing.EndPoints.Massage.Command.AddMassage;

public class AddMassageQuery : IRequest<object>
{
    public int TicketId { get; set; }
    public string Text { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.UserId)]
    public int UserId { get; set; }
    [Ticketing.Services.CallerIdentity(Ticketing.Services.CallerIdentityKind.Username)]
    public string Username { get; set; }
    public IFormFile? File { get; set; }
}