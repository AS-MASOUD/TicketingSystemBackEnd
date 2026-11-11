using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.EndPoints.Ticket.DTOs;
using Ticketing.EndPoints.Ticket.Query.GetGroupTicketList;
using Ticketing.EndPoints.Ticket.Query.GetYearTickerInfo;

namespace Ticketing.Tests;

public class HomeChartTests
{
    private readonly PersianCalendar calendar = new();

    private Ticket Row(int owner, int role, DateTime date) => new()
    {
        UserId = owner, CurrentRoleId = role, InsertDate = date,
        RequestTypeId = RequestType.Support, StatusId = 2
    };

    private async Task<List<MonthTicketItem>> Chart(TestTickets tickets, TestCurrentUser user, int year = 1405)
    {
        var handler = new GetYearTickerInfoHandler.Handler(tickets, new TestProjects(),
            new TestStatuses(), NullLogger<GetYearTickerInfoHandler>.Instance, user);
        // Browser-supplied identity must not change the logged-in user's visibility.
        var result = await handler.Handle(new GetYearTickerInfoQuery {
            Date = year, RequestTypeId = 1, UserId = 999, RoleId = 5
        }, default);
        return Assert.IsType<List<MonthTicketItem>>(result);
    }

    [Fact]
    public async Task Employer_SeesOwnTicketsWithoutSeeingOtherEmployersTickets()
    {
        var tickets = new TestTickets();
        var date = calendar.ToDateTime(1405, 8, 20, 12, 0, 0, 0);
        tickets.Items.AddRange(new[] { Row(42, 5, date), Row(99, 1, date) });
        var result = await Chart(tickets, new TestCurrentUser { RoleIds = new[] { 1 } });
        Assert.Equal(12, result.Count);
        Assert.Equal(1, result[7].Value);
        Assert.Equal(1, result.Sum(month => month.Value));
    }

    [Fact]
    public async Task Employer_WithAnAssignedTeamRole_SeesTeamTickets()
    {
        var tickets = new TestTickets();
        tickets.Items.Add(Row(99, 6, calendar.ToDateTime(1405, 1, 1, 0, 0, 0, 0)));
        var result = await Chart(tickets, new TestCurrentUser { RoleIds = new[] { 1, 6 } });
        Assert.Equal(1, result[0].Value);
    }

    [Fact]
    public async Task Administrator_SeesNewTicketsFromOtherUsers()
    {
        var tickets = new TestTickets();
        tickets.Items.Add(Row(99, 4, calendar.ToDateTime(1405, 2, 1, 0, 0, 0, 0)));
        var result = await Chart(tickets, new TestCurrentUser { RoleIds = new[] { 5 } });
        Assert.Equal(1, result[1].Value);
    }

    [Fact]
    public async Task YearRange_IncludesLastDayAndExcludesNextYear()
    {
        var tickets = new TestTickets();
        var nextYear = calendar.ToDateTime(1407, 1, 1, 0, 0, 0, 0);
        tickets.Items.AddRange(new[] { Row(42, 5, nextYear.AddTicks(-1)), Row(42, 5, nextYear) });
        var result = await Chart(tickets, new TestCurrentUser { RoleIds = new[] { 1 } }, 1406);
        Assert.Equal(1, result[11].Value);
        Assert.Equal(1, result.Sum(month => month.Value));
    }

    [Fact]
    public async Task EmployerWithoutTickets_ReceivesTwelveZeroMonths()
    {
        var result = await Chart(new TestTickets(), new TestCurrentUser { RoleIds = new[] { 1 } });
        Assert.Equal(12, result.Count);
        Assert.All(result, month => Assert.Equal(0, month.Value));
    }
}
