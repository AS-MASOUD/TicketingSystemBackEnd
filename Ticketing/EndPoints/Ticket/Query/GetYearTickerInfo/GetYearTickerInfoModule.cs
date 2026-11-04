using Carter;
using MediatR;

namespace Ticketing.EndPoints.Ticket.Query.GetYearTickerInfo;

public class GetYearTickerInfoModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "api/v1/getYearTicketInfo",
                async (IMediator mediator, [AsParameters] GetYearTickerInfoQuery query,
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
