using Carter;
using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Services;

namespace Ticketing.EndPoints.Ticket.Query.DownloadFile;

public class DownloadFileresponce
{
    public string? filePath { get; set; }
    public string? originalFileName { get; set; }
}

public class DownloadFileQuery : IRequest<DownloadFileresponce?>
{
    public int ticketId { get; set; }
}

public class DownloadFile
{
    public class Handler(ITicketService ticketService, ICurrentUserService currentUser, ILogger<DownloadFile> logger)
        : IRequestHandler<DownloadFileQuery, DownloadFileresponce?>
    {
        public async Task<DownloadFileresponce?> Handle(DownloadFileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var ticket = await ticketService.GetAsync(a => a.Id == request.ticketId);
                if (ticket == null || string.IsNullOrWhiteSpace(ticket.FilePath))
                    return null;

                // Authorization check: User can download if they are adminVira (5), adminTaz (4), or the ticket owner/assignee
                if (!TicketAccessPolicy.CanView(ticket, currentUser))
                {
                    logger.LogWarning("Unauthorized file download attempt by user {UserId} for ticket {TicketId}", currentUser.UserId, request.ticketId);
                    return null;
                }

                string safeFileName = Path.GetFileName(ticket.FilePath);
                string basePath = Path.Combine(Directory.GetCurrentDirectory(), "Files");
                string fullPath = Path.Combine(basePath, safeFileName);

                return new DownloadFileresponce
                {
                    filePath = fullPath,
                    originalFileName = safeFileName
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in DownloadFile Handler for ticket {TicketId}", request.ticketId);
                return null;
            }
        }
    }
}

public class DownloadFileModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(
                "api/v1/downloadFile",
                async (IMediator mediator, [AsParameters] DownloadFileQuery query,
                    CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(query, cancellationToken);
                    if (result != null && !string.IsNullOrEmpty(result.filePath) && System.IO.File.Exists(result.filePath))
                    {
                        return Results.File(System.IO.File.OpenRead(result.filePath), "application/octet-stream", result.originalFileName ?? Path.GetFileName(result.filePath));
                    }
                    return Results.NotFound(new { message = "فایل مورد نظر یافت نشد." });
                })
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("Ticket")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
