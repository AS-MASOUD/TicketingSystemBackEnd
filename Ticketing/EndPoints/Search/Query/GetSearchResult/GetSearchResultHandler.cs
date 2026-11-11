using System.Linq.Expressions;
using System.Globalization;
using MediatR;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.EndPoints.Ticket.Query.GetGroupTicketList;
using Ticketing.Services;
using TicketEntity = Ticketing.Domain.Entities.Ticket;

namespace Ticketing.EndPoints.Search.Query.GetSearchResult;

public class GetSearchResultHandler
{
    public class Handler(ITicketService ticketService, IProjectService projectService, IStatusService statusService,
        ILogger<GetRoleTicketListHandler> logger, ICurrentUserService currentUser)
        : IRequestHandler<GetSearchResultQuery, object>
    {
        public async Task<object> Handle(GetSearchResultQuery request, CancellationToken cancellationToken)
        {
            var number = (request.TicketNumber ?? "").Trim();
            var title = (request.Title ?? "").Trim();
            var username = (request.Username ?? "").Trim();
            if (number.Length > 100 || title.Length > 500 || username.Length > 200)
                return Results.BadRequest(new { message = "متن جستجو بیش از حد طولانی است." });
            int[]? Parse(string? value, bool zeroIsValue = false)
            {
                if (string.IsNullOrWhiteSpace(value)) return null;
                if (value.Length > 1024) throw new ArgumentException();
                var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 100) throw new ArgumentException();
                var ids = parts.Select(p => int.TryParse(p, out int id) && id >= 0 ? id : throw new ArgumentException()).Distinct().ToArray();
                return ids.Length == 0 || !zeroIsValue && ids.Contains(0) ? null : ids;
            }

            int[]? inserted, current, statuses, projects, types, developers, schedules;
            try {
                inserted = Parse(request.InsertedRoleId); current = Parse(request.CurrentRoleId);
                statuses = Parse(request.StatusId); projects = Parse(request.ProjectId); types = Parse(request.RequestType);
                developers = Parse(request.DeveloperId); schedules = Parse(request.IsSchadule, true);
            } catch (ArgumentException) {
                return Results.BadRequest(new { message = "فیلترهای عددی جستجو معتبر نیستند." });
            }
            DateTime? Start(DateTime? value) => value == DateTime.MinValue ? null : value?.Date;
            var from = Start(request.InsertStartDateTime);
            var to = Start(request.InsertEndDateTime);
            var closedFrom = Start(request.CloseStartDateTime);
            var closedTo = Start(request.CloseEndDateTime);
            if (to == DateTime.MaxValue.Date || closedTo == DateTime.MaxValue.Date || from > to || closedFrom > closedTo)
                return Results.BadRequest(new { message = "بازه تاریخ معتبر نیست." });
            var until = to?.AddDays(1);
            var closedUntil = closedTo?.AddDays(1);
            bool wildcard = number.Contains('*');
            number = number.Replace("*", "");
            Expression<Func<TicketEntity, bool>> filters = t =>
                (number == "" || (wildcard ? t.TicketNumber.Contains(number) : t.TicketNumber == number)) &&
                (title == "" || t.Title.Contains(title)) &&
                (username == "" || t.Username.Contains(username)) &&
                (inserted == null || inserted.Contains(t.InsertedRoleId)) &&
                (current == null || current.Contains(t.CurrentRoleId)) &&
                (statuses == null || statuses.Contains(t.StatusId)) &&
                (projects == null || projects.Contains(t.ProjectId)) &&
                (types == null || t.RequestTypeId.HasValue && types.Contains((int)t.RequestTypeId.Value)) &&
                (developers == null || developers.Contains((int)t.DeveloperId)) &&
                (schedules == null || schedules.Contains((int)t.IsSchedule)) &&
                (!from.HasValue || t.InsertDate >= from.Value) &&
                (!until.HasValue || t.InsertDate < until.Value) &&
                (!closedFrom.HasValue || t.ProcessEndDateTime.HasValue && t.ProcessEndDateTime.Value >= closedFrom.Value) &&
                (!closedUntil.HasValue || t.ProcessEndDateTime.HasValue && t.ProcessEndDateTime.Value < closedUntil.Value);
            var visibility = TicketVisibility.For(currentUser);
            var body = new ParameterSubstitution(filters.Parameters[0], visibility.Parameters[0]).Visit(filters.Body)!;
            var predicate = Expression.Lambda<Func<TicketEntity, bool>>(Expression.AndAlso(visibility.Body, body), visibility.Parameters);
            var result = await ticketService.ListAsync(predicate);
            var statusNames = (await statusService.ListAsync(null)).ToDictionary(s => s.Id, s => s.Name);
            var projectNames = (await projectService.ListAsync(null)).ToDictionary(p => p.Id, p => p.Name);
            var p = new PersianCalendar();
            string Date(DateTime value) => $"{p.GetYear(value)}/{p.GetMonth(value):00}/{p.GetDayOfMonth(value):00}";
            return result.OrderByDescending(t => t.InsertDate).ThenByDescending(t => t.Id).Select(t => new {
                Id = t.Id, t.TicketRowNumber, t.TicketNumber, t.Title, t.StatusId,
                Status = statusNames.GetValueOrDefault(t.StatusId, "نامشخص"), t.Username,
                InsertDate = Date(t.InsertDate),
                InsertDateOrder = t.InsertDate,
                ProcessEndDateTime = t.ProcessEndDateTime.HasValue ? Date(t.ProcessEndDateTime.Value) : "ثبت نشده",
                Project = projectNames.GetValueOrDefault(t.ProjectId, "نامشخص"), t.Priority,
                t.InsertedRoleId, t.CurrentRoleId, RequestType = t.RequestTypeId,
                TicketTime = t.TicketTime ?? "0", t.DeveloperId, t.IsSchedule,
                CanEdit = TicketAccessPolicy.CanEdit(t, currentUser),
                AllowedStatuses = Enumerable.Range(1, 9).Where(id => TicketAccessPolicy.CanChangeStatus(t, currentUser, id)).ToArray()
            }).ToList();
        }
    }

    private sealed class ParameterSubstitution(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}
