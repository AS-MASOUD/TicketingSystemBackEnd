using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ticketing.EndPoints.Reporting.Query.GetReport;

namespace Ticketing.EndPoints.Reporting.Query.DownloadReport;

public class GetReportModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "api/v1/downloadReport",
                async (IMediator mediator, [AsParameters] GetReportQuery query,
                    CancellationToken cancellationToken) =>
                {
                    return await mediator.Send(query, cancellationToken);
                })
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("Report")
            .Produces<FileContentResult>();
    }
}
