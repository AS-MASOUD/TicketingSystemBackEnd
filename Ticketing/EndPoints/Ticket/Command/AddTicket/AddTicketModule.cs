using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.EndPoints.Ticket.Command.AddTicket;

public class AddTicketModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "api/v1/addTicket",
                async (IMediator mediator, [FromForm] AddTicketQuery query,
                    CancellationToken cancellationToken) =>
                {
                    return await mediator.Send(query, cancellationToken);
                })
            .RequireAuthorization()
            .DisableAntiforgery()
            .WithOpenApi()
            .WithTags("Ticket")
            .Produces<object[]>();
    }
}
