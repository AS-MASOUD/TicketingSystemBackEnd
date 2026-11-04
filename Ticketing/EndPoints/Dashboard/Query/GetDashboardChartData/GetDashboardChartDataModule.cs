using Carter;
using MediatR;

namespace Ticketing.EndPoints.Dashboard.Query.GetDashboardChartData
{
    public class GetDashboardChartDataModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet(
                    "api/v1/getDashboardChartData",
                    async (IMediator mediator, [AsParameters] GetDashboardChartDataQuery query,
                        CancellationToken cancellationToken) =>
                    {
                        return await mediator.Send(query, cancellationToken);
                    })
                .RequireAuthorization()
                .WithOpenApi()
                .WithTags("Dashboard")
                .Produces<object[]>();
        }
    }
}
