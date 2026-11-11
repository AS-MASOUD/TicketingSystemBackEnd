using System.Globalization;
using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Services;

namespace Ticketing.EndPoints.Massage.Query.GetMassage;

public class GetTicketMassageListHandler
{
    public class Handler(
        IMassageService massageService,
        ITicketService ticketService,
        IStatusService statusService,
        IHttpContextAccessor httpContextAccessor,
        IProjectService projectService,
        ICurrentUserService currentUser,
        ILogger<GetTicketMassageListQuery> _logger)
        : IRequestHandler<GetTicketMassageListQuery, object?>
    {
        public async Task<object?> Handle(GetTicketMassageListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var ticketinfo = await ticketService.GetAsync(a => a.Id == request.TicketId);
                if (ticketinfo == null)
                    return Results.NotFound(new { message = "تیکت یافت نشد." });

                // Server-side authorization check:
                if (!TicketAccessPolicy.CanView(ticketinfo, currentUser))
                {
                    _logger.LogWarning("Unauthorized access attempt to ticket {TicketId} by user {UserId}", request.TicketId, currentUser.UserId);
                    return Results.Json(new { message = "دسترسی به این تیکت مجاز نیست." }, statusCode: 403);
                }

                var liststatus = await statusService.ListAsync(null);
                var listprojects = await projectService.ListAsync(null);
                var messageList = await massageService.ListAsync(a => a.TicketId == request.TicketId);
                var p = new PersianCalendar();

                string scheme = httpContextAccessor.HttpContext?.Request.Scheme ?? "http";
                string host = httpContextAccessor.HttpContext?.Request.Host.Value ?? "localhost";
                string baseFileUrl = $"{scheme}://{host}/Files/";

                bool ticketHasFile = !string.IsNullOrWhiteSpace(ticketinfo.FilePath);

                var statusObj = liststatus.FirstOrDefault(a => a.Id == ticketinfo.StatusId);
                var projectObj = listprojects.FirstOrDefault(a => a.Id == ticketinfo.ProjectId);

                return new
                {
                    permissions = new {
                        canReply = TicketAccessPolicy.CanReply(ticketinfo, currentUser),
                        canEdit = TicketAccessPolicy.CanEdit(ticketinfo, currentUser),
                        canAssign = TicketAccessPolicy.CanAssign(ticketinfo, currentUser),
                        allowedStatuses = Enumerable.Range(1, 9).Where(id => TicketAccessPolicy.CanChangeStatus(ticketinfo, currentUser, id)).ToArray(),
                        allowedDestinations = new[] { 4, 5 }.Where(id => TicketAccessPolicy.CanRoute(ticketinfo, currentUser, id)).ToArray()
                    },
                    ticketInfo = new
                    {
                        Id = ticketinfo.Id,
                        TicketNumber = ticketinfo.TicketNumber,
                        Title = ticketinfo.Title,
                        StatusId = ticketinfo.StatusId,
                        Status = statusObj?.Name ?? "نامشخص",
                        Username = ticketinfo.Username,
                        Date = p.GetHour(ticketinfo.InsertDate).ToString("00") + ":" + p.GetMinute(ticketinfo.InsertDate).ToString("00") + " - " + p.GetYear(ticketinfo.InsertDate) + "/" + p.GetMonth(ticketinfo.InsertDate).ToString("00") + "/" + p.GetDayOfMonth(ticketinfo.InsertDate).ToString("00"),
                        Project = projectObj?.Name ?? "نامشخص",
                        ProjectId = projectObj?.Id ?? ticketinfo.ProjectId,
                        Priority = ticketinfo.Priority,
                        Text = ticketinfo.Text,
                        File = ticketHasFile ? baseFileUrl + ticketinfo.FilePath : "",
                        UserId = ticketinfo.UserId,
                        HaveFile = ticketHasFile,
                        TicketTime = ticketinfo.TicketTime,
                        DeveloperId = ticketinfo.DeveloperId,
                        RequestTypeId = ticketinfo.RequestTypeId,
                        IsSchedule = ticketinfo.IsSchedule,
                    },
                    messageList = messageList.Select(x =>
                    {
                        bool msgHasFile = !string.IsNullOrWhiteSpace(x.FilePath);
                        return new
                        {
                            Id = x.Id,
                            Text = x.Text,
                            Username = x.Username,
                            UserId = x.UserId,
                            Date = p.GetHour(x.InsertDate).ToString("00") + ":" + p.GetMinute(x.InsertDate).ToString("00") + " - " + p.GetYear(x.InsertDate) + "/" + p.GetMonth(x.InsertDate).ToString("00") + "/" + p.GetDayOfMonth(x.InsertDate).ToString("00"),
                            FilePath = msgHasFile ? baseFileUrl + x.FilePath : "",
                            HaveFile = msgHasFile
                        };
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTicketMassageListHandler for TicketId {TicketId}", request.TicketId);
            }

            return null;
        }
    }
}
