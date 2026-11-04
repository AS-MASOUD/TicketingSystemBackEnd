using Carter;
using MediatR;

namespace Ticketing.EndPoints.Ticket.Query.GetGroupTicketList;

public class GetRoleTicketListModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "api/v1/getTicketList",
                async (IMediator mediator, [AsParameters] GetRoleTicketListQuery query,
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
