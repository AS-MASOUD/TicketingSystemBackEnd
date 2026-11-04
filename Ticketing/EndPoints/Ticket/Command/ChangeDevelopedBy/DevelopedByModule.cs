using Carter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ticketing.EndPoints.Ticket.Command.ChangeDevelopedBy
{
    public class DevelopedByModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost(
                    "api/v1/changeDevelopedBy",
                    async (IMediator mediator, [FromBody] DevelopedByQuery query,
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
}
