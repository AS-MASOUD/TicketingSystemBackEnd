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
                if (!currentUser.IsAuthenticated || currentUser.UserId is not > 0)
                    return Results.Unauthorized();
                if (currentUser.RoleIds.All(r => r == 3))
                    return Results.Json(new { message = "نقش ناظر فقط دسترسی مشاهده دارد." }, statusCode: 403);
                if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 1000 ||
                    string.IsNullOrWhiteSpace(request.Text) || request.Text.Length > 100000 ||
                    (int)request.Priority is < 1 or > 3 || (int)request.RequestType is < 1 or > 2)
                    return Results.BadRequest(new { message = "عنوان، متن، اولویت و نوع درخواست معتبر الزامی است." });
                if (await projectService.GetAsync(p => p.Id == request.ProjectId) == null)
                    return Results.BadRequest(new { message = "سامانه انتخاب‌شده وجود ندارد." });
                request.UserId = currentUser.UserId.Value;
                request.Username = currentUser.Username ?? "";
                if (!currentUser.HasRole(request.RoleId))
                    request.RoleId = currentUser.DefaultRoleId ?? 1;

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
                        return Results.BadRequest(new { message = "نوع فایل مجاز نیست." });
                    }
                }

                var persiandate = new System.Globalization.PersianCalendar();
                var rowNumber = await ticketService.GetNextRowNumberAsync();
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
                                   rowNumber.ToString("000#"),
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
                return Results.Json(new { message = "خطا در ثبت تیکت: " + ex.Message }, statusCode: 500);
            }
        }
    }
}
