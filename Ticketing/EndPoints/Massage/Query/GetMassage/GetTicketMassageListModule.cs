using Carter;
using MediatR;

namespace Ticketing.EndPoints.Massage.Query.GetMassage;

public class GetTicketMassageListModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "api/v1/getTicketMassages",
                async (IMediator mediator, [AsParameters] GetTicketMassageListQuery query,
                    CancellationToken cancellationToken) =>
                {
                    return await mediator.Send(query, cancellationToken);
                })
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("Massage")
            .Produces<object[]>();
    }
}
