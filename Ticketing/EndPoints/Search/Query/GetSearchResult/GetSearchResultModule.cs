using Carter;
using MediatR;

namespace Ticketing.EndPoints.Search.Query.GetSearchResult;

public class GetSearchResultModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "api/v1/search",
                async (IMediator mediator, [AsParameters] GetSearchResultQuery query,
                    CancellationToken cancellationToken) =>
                {
                    return await mediator.Send(query, cancellationToken);
                })
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("Search")
            .Produces<object[]>();
    }
}
