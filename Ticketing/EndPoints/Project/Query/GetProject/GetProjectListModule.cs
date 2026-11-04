using Carter;
using MediatR;

namespace Ticketing.EndPoints.Group.Query.GetGroup;

public class GetProjectListModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "api/v1/getProjects",
                async (IMediator mediator, [AsParameters] GetProjectListQuery query,
                    CancellationToken cancellationToken) =>
                {
                    return await mediator.Send(query, cancellationToken);
                })
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("Project")
            .Produces<object[]>();
    }
}
