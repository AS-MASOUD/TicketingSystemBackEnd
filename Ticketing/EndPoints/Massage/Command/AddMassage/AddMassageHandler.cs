using MediatR;
using Ticketing.Application.Service.Massage;
using Ticketing.Domain.Contracts;
using Ticketing.Services;

namespace Ticketing.EndPoints.Massage.Command.AddMassage;

public class AddMassageHandler
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".zip", ".rar", ".txt", ".xlsx", ".xls", ".docx", ".doc"
    };

    public class Handler(
        IMassageService massageService,
        ITicketService ticketService,
        ICurrentUserService currentUser,
        ILogger<AddMassageHandler> _logger)
        : IRequestHandler<AddMassageQuery, object?>
    {
        public async Task<object?> Handle(AddMassageQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Enforce authenticated user identity
                if (currentUser.IsAuthenticated && currentUser.UserId.HasValue)
                {
                    if (!currentUser.IsAdminVira)
                    {
                        request.UserId = currentUser.UserId.Value;
                        if (!string.IsNullOrEmpty(currentUser.Username))
                            request.Username = currentUser.Username;
                    }
                }

                string fileName = "";
                if (request.File != null && request.File.Length > 0)
                {
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "Files");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string ext = Path.GetExtension(request.File.FileName);
                    if (!string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext))
                    {
                        fileName = Guid.NewGuid() + ext;
                        string fileNameWithPath = Path.Combine(path, fileName);
                        using (var stream = new FileStream(fileNameWithPath, FileMode.Create))
                        {
                            await request.File.CopyToAsync(stream, cancellationToken);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Message attachment rejected due to disallowed extension: {Extension}", ext);
                    }
                }

                return await massageService.AddAsync(new Domain.Entities.Massage
                {
                    Text = request.Text ?? string.Empty,
                    TicketId = request.TicketId,
                    InsertDate = DateTime.Now,
                    UserId = request.UserId,
                    Username = request.Username ?? string.Empty,
                    FilePath = fileName,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddMassageHandler Handle");
            }

            return null;
        }
    }
}
