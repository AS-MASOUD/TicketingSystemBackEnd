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
                    var report = await mediator.Send(query, cancellationToken);
                    if (report == null) return Results.BadRequest(new { message = "فیلترهای گزارش معتبر نیستند." });
                    return Results.File(report.FileContents, report.ContentType, report.FileDownloadName);
                })
            .RequireAuthorization(policy => policy.RequireRole("4", "5"))
            .WithOpenApi()
            .WithTags("Report")
            .Produces<FileContentResult>();
    }
}
