using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Services;

namespace Ticketing.EndPoints.Ticket.Query.GetGroupTicketList;

public class GetRoleTicketListHandler
{
    public class Handler(
        ITicketService ticketService,
        IProjectService projectService,
        IStatusService statusService,
        ICurrentUserService currentUser,
        ILogger<GetRoleTicketListHandler> _logger)
        : IRequestHandler<GetRoleTicketListQuery, object?>
    {
        public async Task<object?> Handle(GetRoleTicketListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var liststatus = await statusService.ListAsync(null);
                var listProject = await projectService.ListAsync(null);
                var visible = await ticketService.ListAsync(TicketVisibility.For(currentUser));
                var result = visible
                    .Where(t => !request.RequestTypeId.HasValue || request.RequestTypeId <= 0 || (int?)t.RequestTypeId == request.RequestTypeId)
                    .Where(t => !request.Status.HasValue || request.Status <= 0 ||
                        (request.Status == 8 && !currentUser.IsAdminVira && !currentUser.IsAdminTaz
                            ? t.StatusId is not (1 or 2 or 4) : t.StatusId == request.Status))
                    .ToList();
                var persiandate = new System.Globalization.PersianCalendar();

                return result.OrderByDescending(a => a.TicketNumber).Select(x => new
                {
                    Id = x.Id,
                    TicketRowNumber = x.TicketRowNumber,
                    TicketNumber = x.TicketNumber,
                    Title = x.Title,
                    StatusId = liststatus.FirstOrDefault(a => a.Id == x.StatusId)?.Id ?? x.StatusId,
                    Status = liststatus.FirstOrDefault(a => a.Id == x.StatusId)?.Name ?? "نامشخص",
                    Username = x.Username,
                    Date = persiandate.GetYear(x.InsertDate) + "/" + persiandate.GetMonth(x.InsertDate).ToString("D2") + "/" + persiandate.GetDayOfMonth(x.InsertDate).ToString("D2"),
                    Project = listProject.FirstOrDefault(a => a.Id == x.ProjectId)?.Name ?? "نامشخص",
                    Priority = x.Priority,
                    InsertedRoleId = x.InsertedRoleId,
                    CurrentRoleId = x.CurrentRoleId,
                    RequestType = x.RequestTypeId,
                    DeveloperId = x.DeveloperId,
                    DeveloperName = GetDeveloperName((int)x.DeveloperId),
                    CanEdit = TicketAccessPolicy.CanEdit(x, currentUser),
                    TicketTime = x.TicketTime
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRoleTicketListHandler Handle");
                return Array.Empty<object>();
            }
        }

        private static string GetDeveloperName(int devId) => devId switch
        {
            1 => "پویا رضاییه",
            2 => "محمد باقری",
            3 => "توحید حقیقی",
            4 => "مهسا برجی",
            5 => "امیر مسعود صالحی",
            6 => "شکیلا کاظم پور",
            7 => "احسان درویشی",
            8 => "الهه ابراهیمی",
            9 => "ساناز محمد زاده",
            10 => "تخصیص‌نشده",
            _ => (devId > 0 ? $"کارشناس #{devId}" : "تخصیص‌نشده")
        };
    }
}
