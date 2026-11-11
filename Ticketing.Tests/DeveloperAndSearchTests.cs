using Ticketing.Domain.Enums;
using Ticketing.EndPoints.Search.Query.GetSearchResult;
using Ticketing.EndPoints.Ticket.Command.ChangeDevelopedBy;
using static Ticketing.Tests.TicketTestFixture;

namespace Ticketing.Tests;

public class DeveloperAndSearchTests
{
    [Fact]
    public void ActiveDeveloperChoices_ContainOnlyTheTwoRequestedPeople()
    {
        Assert.Equal(new[] { 9, 5 }, DeveloperCatalog.Active.Select(d => d.Id));
        Assert.Equal(new[] { "ساناز محمدزاده", "امیرمسعود صالحی" }, DeveloperCatalog.Active.Select(d => d.Name));
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(9, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(6, false)]
    [InlineData(7, false)]
    [InlineData(8, false)]
    [InlineData(10, false)]
    public async Task Assignment_OnlyAcceptsActiveDevelopers(int developer, bool allowed)
    {
        var f = new TicketTestFixture();
        f.User.RoleIds = new[] { 5 };
        var result = await f.Handler<DevelopedByHandler.Handler>().Handle(new DevelopedByQuery {
            TicketId = 2, DeveloperId = (Developer)developer, Time = "15"
        }, default);
        Assert.Equal(allowed, !Denied(result));
        Assert.Equal(allowed ? 1 : 0, f.Tickets.Writes);
    }

    [Fact]
    public async Task BlankSearch_ReturnsAllVisibleTicketsInActualCreationDateOrder()
    {
        var f = new TicketTestFixture();
        f.User.RoleIds = new[] { 5 };
        f.Tickets.Items[0].InsertDate = new DateTime(2026, 11, 10, 15, 0, 0);
        f.Tickets.Items[1].InsertDate = new DateTime(2026, 11, 10, 9, 0, 0);
        f.Tickets.Items[2].InsertDate = new DateTime(2026, 11, 9, 22, 0, 0);
        var result = await f.Handler<GetSearchResultHandler.Handler>().Handle(new GetSearchResultQuery(), default);
        var rows = Json(result).EnumerateArray().ToArray();
        Assert.Equal(new[] { 1, 2, 3 }, rows.Select(row => row.GetProperty("Id").GetInt32()));
        Assert.All(rows, row => Assert.True(row.TryGetProperty("InsertDateOrder", out _)));
    }

    [Fact]
    public async Task BlankSearch_StillRestrictsEmployerToVisibleTickets()
    {
        var f = new TicketTestFixture();
        f.User.RoleIds = new[] { 1 };
        var result = await f.Handler<GetSearchResultHandler.Handler>().Handle(new GetSearchResultQuery(), default);
        var rows = Json(result).EnumerateArray().ToArray();
        Assert.Single(rows);
        Assert.Equal(1, rows[0].GetProperty("Id").GetInt32());
    }
}
