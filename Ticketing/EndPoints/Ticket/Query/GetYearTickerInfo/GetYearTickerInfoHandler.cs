using MediatR;
using System.Globalization;
using Ticketing.Application.Service.Stauts;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.EndPoints.Ticket.DTOs;
using Ticketing.EndPoints.Ticket.Query.GetYearTickerInfo;
using Ticketing.Services;

namespace Ticketing.EndPoints.Ticket.Query.GetGroupTicketList;
public class GetYearTickerInfoHandler
{
    public class Handler(ITicketService ticketService,IProjectService projectService ,IStatusService statusService,ILogger<GetYearTickerInfoHandler> _logger, ICurrentUserService currentUser):IRequestHandler<GetYearTickerInfoQuery, object>
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
                var year = request.Date ?? p.GetYear(DateTime.Today);
                DateTime from, until;
                try
                {
                    from = p.ToDateTime(year, 1, 1, 0, 0, 0, 0);
                    until = p.ToDateTime(year + 1, 1, 1, 0, 0, 0, 0);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return Results.BadRequest(new { message = "سال شمسی معتبر نیست." });
                }
                var result = (await ticketService.ListAsync(TicketVisibility.For(currentUser)))
                    .Where(t => (request.RequestTypeId <= 0 || (int?)t.RequestTypeId == request.RequestTypeId)
                        && t.InsertDate >= from && t.InsertDate < until).ToList();

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
