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
                    return null;

                // Server-side authorization check:
                // User can view ticket details if they are adminVira (5), adminTaz (4), the creator, or assigned to current role
                if (currentUser.IsAuthenticated)
                {
                    if (!currentUser.IsAdminVira && !currentUser.IsAdminTaz &&
                        ticketinfo.UserId != currentUser.UserId &&
                        !currentUser.HasRole(ticketinfo.CurrentRoleId))
                    {
                        _logger.LogWarning("Unauthorized access attempt to ticket {TicketId} by user {UserId}", request.TicketId, currentUser.UserId);
                        return null;
                    }
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
