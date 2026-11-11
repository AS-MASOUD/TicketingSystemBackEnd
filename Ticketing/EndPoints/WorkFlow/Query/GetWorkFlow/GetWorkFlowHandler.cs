using MediatR;
using Ticketing.Services;
using System.Globalization;
using Ticketing.Application.Service.Stauts;
using Ticketing.Domain.Contracts;
using Ticketing.EndPoints.Ticket.Query.GetGroupTicketInfo;

namespace Ticketing.EndPoints.WorkFlow.Query.GetWorkFlow
{
    public class GetWorkFlowHandler
    {
        public class Handler(ITicketFlowService ticketFlowService, IStatusService statusService , ILogger<GetRoleTicketInfoHandler> _logger, ITicketService ticketService, ICurrentUserService currentUser) : IRequestHandler<GetWorkFlowQuery, object>
        {
            public async Task<object> Handle(GetWorkFlowQuery query, CancellationToken cancellationToken)
            {
                try
                {
                    var ticket = await ticketService.GetAsync(t => t.Id == query.TicketId);
                    if (!TicketAccessPolicy.CanView(ticket, currentUser))
                        return Results.Json(new { message = "دسترسی به گردش کار مجاز نیست." }, statusCode: 403);
                    var ticketFlowList = await ticketFlowService.ListAsync(a => a.TicketId == query.TicketId);
                    var liststatus = await statusService.ListAsync(null);

                    var p = new PersianCalendar();
                    return ticketFlowList.OrderBy(a => a.InsertDate).Select(x => new
                    {
                        flowId = x.Id,
                        ticketId = x.TicketId,
                        userId = x.UserId,// Ticket belongs to this user
                        userName = x.Username,// Made by this user name
                        StatusId = liststatus.FirstOrDefault(a => a.Id == x.StatusId)?.Id ?? x.StatusId,
                        Status = liststatus.FirstOrDefault(a => a.Id == x.StatusId)?.Name ?? "نامشخص",
                        currentRoleId = x.CurrentRoleId,
                        previousRoleId = x.PreviousRoleId,
                        insertedDate = p.GetHour(x.InsertDate) + ":" +
                                      (p.GetMinute(x.InsertDate) < 10 ? ("0" + p.GetMinute(x.InsertDate)) : p.GetMinute(x.InsertDate)) + " - " +
                                       p.GetYear(x.InsertDate) + "/" +
                                       p.GetMonth(x.InsertDate) + "/" +
                                       p.GetDayOfMonth(x.InsertDate), // 2024/01/01 19:4 -> 2024/01/01 19:04,
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Get Error : GetTicketMassageList- Handle " + ex.Message);
                }
                return null;
            }
        }
    }
}
