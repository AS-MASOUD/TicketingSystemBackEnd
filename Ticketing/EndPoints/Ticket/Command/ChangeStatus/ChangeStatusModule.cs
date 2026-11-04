using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.EndPoints.Ticket.Command.ChangeStatus;

public class ChangeStatusModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "api/v1/changeStatus",
                async (IMediator mediator, [FromBody] ChangeStatusQuery query,
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
