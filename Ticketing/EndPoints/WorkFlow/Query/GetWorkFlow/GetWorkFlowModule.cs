using Carter;
using MediatR;

namespace Ticketing.EndPoints.WorkFlow.Query.GetWorkFlow;

public class GetWorkFlowModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "api/v1/getWorkFlow",
                async (IMediator mediator, [AsParameters] GetWorkFlowQuery query,
                    CancellationToken cancellationToken) =>
                {
                    return await mediator.Send(query, cancellationToken);
                })
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("WorkFlow")
            .Produces<object[]>();
    }
}
