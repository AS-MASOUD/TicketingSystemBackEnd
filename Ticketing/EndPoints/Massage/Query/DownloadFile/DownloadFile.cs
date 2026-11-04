using Carter;
using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Services;

namespace Ticketing.EndPoints.Massage.Query.DownloadFile;

public class MasaageDownloadFileResponce
{
    public string? filePath { get; set; }
    public string? originalFileName { get; set; }
}

public class MassageDownloadFileQuery : IRequest<MasaageDownloadFileResponce?>
{
    public int massageId { get; set; }
}

public class DownloadMessageFile
{
    public class Handler(IMassageService massageService, ITicketService ticketService, ICurrentUserService currentUser, ILogger<DownloadMessageFile> logger)
        : IRequestHandler<MassageDownloadFileQuery, MasaageDownloadFileResponce?>
    {
        public async Task<MasaageDownloadFileResponce?> Handle(MassageDownloadFileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var massage = await massageService.GetAsync(a => a.Id == request.massageId);
                if (massage == null || string.IsNullOrWhiteSpace(massage.FilePath))
                    return null;

                var ticket = await ticketService.GetAsync(a => a.Id == massage.TicketId);
                if (ticket != null)
                {
                    if (!currentUser.IsAdminVira && !currentUser.IsAdminTaz && ticket.UserId != currentUser.UserId && !currentUser.HasRole(ticket.CurrentRoleId))
                    {
                        logger.LogWarning("Unauthorized message file download attempt by user {UserId} for massage {MassageId}", currentUser.UserId, request.massageId);
                        return null;
                    }
                }

                string safeFileName = Path.GetFileName(massage.FilePath);
                string basePath = Path.Combine(Directory.GetCurrentDirectory(), "Files");
                string fullPath = Path.Combine(basePath, safeFileName);

                return new MasaageDownloadFileResponce
                {
                    filePath = fullPath,
                    originalFileName = safeFileName
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in DownloadMessageFile Handler for message {MassageId}", request.massageId);
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
                "api/v1/downloadMassageFile",
                async (IMediator mediator, [AsParameters] MassageDownloadFileQuery query,
                    CancellationToken cancellationToken) =>
                {
                    var result = await mediator.Send(query, cancellationToken);
                    if (result != null && !string.IsNullOrEmpty(result.filePath) && System.IO.File.Exists(result.filePath))
                    {
                        return Results.File(System.IO.File.OpenRead(result.filePath), "application/octet-stream", result.originalFileName ?? Path.GetFileName(result.filePath));
                    }
                    return Results.NotFound(new { message = "فایل پیوست پیام یافت نشد." });
                })
            .RequireAuthorization()
            .WithOpenApi()
            .WithTags("Massage")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
