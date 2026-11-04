using Carter;
using MediatR;

namespace Ticketing.EndPoints.Status.Query.GetStatus;

public class GetStatusListModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "api/v1/getStatus",
                async (IMediator mediator, [AsParameters] GetStatusListQuery query,
                    CancellationToken cancellationToken) =>
                {
                    return await mediator.Send(query, cancellationToken);
                })
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("Status")
            .Produces<object[]>();
    }
}
