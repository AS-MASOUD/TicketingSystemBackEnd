using MediatR;
using System.Globalization;
using Ticketing.Application.Service.Stauts;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.EndPoints.Ticket.DTOs;
using Ticketing.EndPoints.Ticket.Query.GetYearTickerInfo;

namespace Ticketing.EndPoints.Ticket.Query.GetGroupTicketList;
public class GetYearTickerInfoHandler
{
    public class Handler(ITicketService ticketService,IProjectService projectService ,IStatusService statusService,ILogger<GetYearTickerInfoHandler> _logger):IRequestHandler<GetYearTickerInfoQuery, object>
    {
        public async Task<object> Handle(GetYearTickerInfoQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var doneList = new List<int>();
                var rejectList = new List<int>();
                var doingList = new List<int>();
                var resultyear = new List<MonthTicketItem>();
                var p = new PersianCalendar();
                var result = await ticketService.ListAsync(a => (a.UserId == request.UserId) ||
                                                                                    a.CurrentRoleId == request.RoleId);
                if (request.RoleId == 4)
                {
                     result = await ticketService.ListAsync(a => (int)a.RequestTypeId == request.RequestTypeId);
                }
                else if (request.RoleId == 5)
                {
                    result = await ticketService.ListAsync(a => (a.StatusId != 2 || a.UserId == request.UserId) && (int)a.RequestTypeId == request.RequestTypeId);
                }
                else
                {
                    result = await ticketService.ListAsync(a => ((a.UserId == request.UserId) || a.CurrentRoleId == request.RoleId) && (int)a.RequestTypeId == request.RequestTypeId);
                }
                result = request.Date switch
                {
                    1403 => result.Where(a => (a.InsertDate >= new DateTime(2024, 3, 20) && a.InsertDate <= new DateTime(2025, 3, 20))).ToList(),
                    1404 => result.Where(a => (a.InsertDate >= new DateTime(2025, 3, 21) && a.InsertDate <= new DateTime(2026, 3, 20))).ToList(),
                    1405 => result.Where(a => (a.InsertDate >= new DateTime(2026, 3, 21) && a.InsertDate <= new DateTime(2027, 3, 20))).ToList(),
                };

                for (var i = 1; i < 13; i++)
                {
                    int done = result.Where(a=>p.GetMonth(a.InsertDate) == i && a.StatusId == 1).Count();
                    int inserted = result.Where(a => p.GetMonth(a.InsertDate) == i && a.StatusId == 2).Count();
                    int sendtovira = result.Where(a=> p.GetMonth(a.InsertDate) == i && a.StatusId == 3).Count();
                    int reject = result.Where(a=> p.GetMonth(a.InsertDate) == i && a.StatusId == 4).Count();
                    int sendtotaz = result.Where(a => p.GetMonth(a.InsertDate) == i && a.StatusId == 5).Count();
                    int awaitingConfirmation = result.Where(a => p.GetMonth(a.InsertDate) == i && a.StatusId == 6).Count();
                    int inLine = result.Where(a => p.GetMonth(a.InsertDate) == i && a.StatusId == 7).Count();
                    int inProgress = result.Where(a => p.GetMonth(a.InsertDate) == i && a.StatusId == 8).Count();
                    int awaitingRejecting = result.Where(a => p.GetMonth(a.InsertDate) == i && a.StatusId == 9).Count();
                    resultyear.Add(new MonthTicketItem() {
                        Month = GetMonth(i),
                        Value = done+inserted+sendtovira+reject+sendtotaz+awaitingConfirmation+inLine+inProgress+ awaitingRejecting,
                    });
                }

                return resultyear;
            }
            catch (Exception ex)
            {
                _logger.LogError("Get Error : GetTicketMassageList- Handle " + ex.Message);
            }

            return null;
        }

        public string GetMonth(int month)
        {
            if (month == 1) return "فروردین";
            else if(month == 2) return "اردیبهشت";
            else if(month == 3) return "خرداد";
            else if(month == 4) return "تیر";
            else if(month == 5) return "مرداد";
            else if(month == 6) return "شهریور";
            else if(month == 7) return "مهر";
            else if(month == 8) return "ابان";
            else if(month == 9) return "اذر";
            else if(month == 10) return "دی";
            else if(month == 11) return "بهمن";
            else if (month == 12) return "اسفند";
            return "";
        }
    }
}