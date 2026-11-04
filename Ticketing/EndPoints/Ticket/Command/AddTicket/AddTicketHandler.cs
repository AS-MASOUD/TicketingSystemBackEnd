using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Enums;
using Ticketing.Services;

namespace Ticketing.EndPoints.Ticket.Command.AddTicket;

public class AddTicketHandler
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".zip", ".rar", ".txt", ".xlsx", ".xls", ".docx", ".doc"
    };

    public class Handler(
        ITicketService ticketService,
        IProjectService projectService,
        ITicketFlowService ticketFlowService,
        ICurrentUserService currentUser,
        ILogger<AddTicketHandler> _logger)
        : IRequestHandler<AddTicketQuery, object?>
    {
        public async Task<object?> Handle(AddTicketQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Server-side identity enforcement: Do not trust browser-supplied UserId/Username blindly
                if (currentUser.IsAuthenticated && currentUser.UserId.HasValue)
                {
                    if (!currentUser.IsAdminVira)
                    {
                        request.UserId = currentUser.UserId.Value;
                        if (!string.IsNullOrEmpty(currentUser.Username))
                            request.Username = currentUser.Username;
                    }
                    if (request.RoleId <= 0 || (!currentUser.HasRole(request.RoleId) && !currentUser.IsAdminVira && !currentUser.IsAdminTaz))
                    {
                        request.RoleId = currentUser.DefaultRoleId ?? 2;
                    }
                }

                string fileName = "";
                if (request.File != null && request.File.Length > 0)
                {
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "Files");
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

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
                        _logger.LogWarning("File rejected due to disallowed extension: {Extension}", ext);
                    }
                }

                var persiandate = new System.Globalization.PersianCalendar();
                var getlastticket = await ticketService.ListAsync(null);
                var rowNumber = (getlastticket.Count == 0 ? 1 : getlastticket.Last().TicketRowNumber + 1);
                if (request.RequestType == RequestType.Support)
                {
                    request.IsSchedule = IsSchedule.Support;
                }

                int effectiveRoleId = request.RoleId > 0 ? request.RoleId : 2;
                bool isAdminTaz = effectiveRoleId == 4;

                var ticket = new Domain.Entities.Ticket
                {
                    CurrentRoleId = isAdminTaz ? 5 : 4,
                    InsertedRoleId = effectiveRoleId,
                    Text = request.Text ?? string.Empty,
                    Title = request.Title ?? string.Empty,
                    Priority = request.Priority,
                    UserId = request.UserId,
                    RequestTypeId = request.RequestType,
                    StatusId = isAdminTaz ? 3 : 2, // 3: Send to vira, 2: New ticket
                    CloseDate = DateTime.Now,
                    InsertDate = DateTime.Now,
                    ProjectId = request.ProjectId,
                    FilePath = fileName,
                    Username = request.Username ?? string.Empty,
                    LastChangeDatetime = null,
                    TicketRowNumber = rowNumber,
                    TicketNumber = persiandate.GetYear(DateTime.Now).ToString() +
                                   persiandate.GetMonth(DateTime.Now).ToString("00") +
                                   persiandate.GetDayOfMonth(DateTime.Now).ToString("00") +
                                   rowNumber.Value.ToString("000#"),
                    DeveloperId = Developer.unknown,
                    IsSchedule = request.IsSchedule ?? IsSchedule.Support
                };

                var result = await ticketService.AddAsync(ticket);

                await ticketFlowService.AddAsync(new Domain.Entities.TicketFlow
                {
                    CurrentRoleId = ticket.CurrentRoleId,
                    InsertDate = DateTime.Now,
                    StatusId = ticket.StatusId,
                    UserId = request.UserId,
                    Username = request.Username ?? string.Empty,
                    TicketId = result.Id,
                    PreviousRoleId = effectiveRoleId
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddTicketHandler Handle");
            }

            return null;
        }
    }
}
