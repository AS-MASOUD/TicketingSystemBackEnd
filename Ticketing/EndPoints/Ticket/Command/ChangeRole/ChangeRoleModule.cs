using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.EndPoints.Ticket.Command.ChangeGroup;

public class ChangeRoleModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost(
                "api/v1/changeRole",
                async (IMediator mediator, [FromBody] ChangeRoleQuery query,
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
