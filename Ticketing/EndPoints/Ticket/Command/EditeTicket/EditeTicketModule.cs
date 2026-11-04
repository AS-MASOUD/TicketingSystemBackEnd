using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.EndPoints.Ticket.Command.EditeTicket;

public class EditeTicketModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "api/v1/EditeTicket",
                async (IMediator mediator, [FromBody] EditeTicketQuery query,
                    CancellationToken cancellationToken) =>
                {
                    return await mediator.Send(query, cancellationToken);
                })
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("Ticket")
            .Produces<object[]>();
    }
}
