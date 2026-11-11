using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Ticketing.Domain.Contracts;
using Ticketing.EndPoints.Reporting.Query.Export;
using Ticketing.EndPoints.Reporting.Query.GetReport;
using Ticketing.Services;

namespace Ticketing.EndPoints.Reporting.Query.DownloadReport;

public class GetReportHandler
{
    public class Handler(ITicketService ticketService, IProjectService projectService, IStatusService statusService,
        IExport export, ICurrentUserService currentUser, ILogger<GetReportHandler> logger)
        : IRequestHandler<GetReportQuery, FileContentResult>
    {
        public async Task<FileContentResult> Handle(GetReportQuery request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAdminTaz && !currentUser.IsAdminVira) return null!;
            try {
                int[]? Parse(string? input, bool zeroIsValue = false) {
                    if (string.IsNullOrWhiteSpace(input)) return null;
                    if (input.Length > 1024) throw new ArgumentException();
                    var ids=input.Split(',', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries)
                        .Select(value=>int.TryParse(value,out int id)&&id>=0?id:throw new ArgumentException()).ToArray();
                    return ids.Length==0 || !zeroIsValue&&ids.Contains(0) ? null : ids;
                }
                var projects=Parse(request.ProjectId);var priorities=Parse(request.Priority);
                var types=Parse(request.RequestType);var states=Parse(request.StatusId);
                var developers=Parse(request.DeveloperId);var schedules=Parse(request.IsSchadule,true);
                var from=request.InsertStartDateTime?.Date;var until=request.InsertEndDateTime?.Date.AddDays(1);
                var closedFrom=request.CloseStartDateTime?.Date;var closedUntil=request.CloseEndDateTime?.Date.AddDays(1);
                if(from>=until || closedFrom>=closedUntil) return null!;
                var tickets=(await ticketService.ListAsync(TicketVisibility.For(currentUser))).Where(t=>
                    (projects==null||projects.Contains(t.ProjectId)) &&
                    (priorities==null||priorities.Contains((int)t.Priority)) &&
                    (types==null||t.RequestTypeId.HasValue&&types.Contains((int)t.RequestTypeId.Value)) &&
                    (states==null||states.Contains(t.StatusId)) &&
                    (developers==null||developers.Contains((int)t.DeveloperId)) &&
                    (schedules==null||schedules.Contains((int)t.IsSchedule)) &&
                    (!from.HasValue||t.InsertDate>=from.Value) && (!until.HasValue||t.InsertDate<until.Value) &&
                    (!closedFrom.HasValue||t.ProcessEndDateTime>=closedFrom.Value) &&
                    (!closedUntil.HasValue||t.ProcessEndDateTime<closedUntil.Value))
                    .OrderBy(t=>t.InsertDate).ToList();
                var projectNames=(await projectService.ListAsync(null)).ToDictionary(p=>p.Id,p=>p.Name);
                var statusNames=(await statusService.ListAsync(null)).ToDictionary(s=>s.Id,s=>s.Name);
                var calendar=new PersianCalendar();
                string Date(DateTime value)=>$"{calendar.GetYear(value)}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00}";
                var headers=new List<string>{"ردیف","شماره تیکت","ثبت‌کننده","تاریخ ثبت","نوع درخواست","برنامه زمانی","اولویت","عنوان","سامانه","وضعیت","انجام‌دهنده","زمان صرف‌شده","تاریخ تحویل"};
                var rows=tickets.Select((t,index)=>new[]{
                    new CellInfo{Text=(index+1).ToString()},new CellInfo{Text=t.TicketNumber},new CellInfo{Text=t.Username},
                    new CellInfo{Text=Date(t.InsertDate)},new CellInfo{Text=(int?)t.RequestTypeId==1?"پشتیبانی":"توسعه"},
                    new CellInfo{Text=(int)t.IsSchedule==1?"بله":(int)t.IsSchedule==0?"خیر":"پشتیبانی"},
                    new CellInfo{Text=(int)t.Priority==1?"بالا":(int)t.Priority==2?"متوسط":"پایین"},
                    new CellInfo{Text=t.Title},new CellInfo{Text=projectNames.GetValueOrDefault(t.ProjectId,t.ProjectId.ToString())},
                    new CellInfo{Text=statusNames.GetValueOrDefault(t.StatusId,t.StatusId.ToString())},
                    new CellInfo{Text=(int)t.DeveloperId==10?"تخصیص‌نشده":t.DeveloperId.ToString()},
                    new CellInfo{Text=t.TicketTime??"0"},new CellInfo{Text=t.ProcessEndDateTime.HasValue?Date(t.ProcessEndDateTime.Value):"ثبت نشده"}
                }).ToList();
                var totals=Enumerable.Repeat("",headers.Count).ToList();
                totals[0]="جمع";
                totals[11]=tickets.Sum(t=>long.TryParse(t.TicketTime,out var value)&&value>0?value:0).ToString(CultureInfo.InvariantCulture);
                return export.ToExcel("Ticket-report-"+DateTime.Now.ToString("yyyy-MM-dd"),headers,
                    Enumerable.Repeat(ExcelDataType.Text,headers.Count).ToList(),rows,totals,new List<string>{"تاریخ گزارش: "+Date(DateTime.Now)});
            } catch(ArgumentException) { return null!; }
        }
    }
}
