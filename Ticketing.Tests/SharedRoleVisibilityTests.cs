using Ticketing.Services;

namespace Ticketing.Tests;

public class SharedRoleVisibilityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void RequestersAndDevelopers_CanViewEveryTicket_RegardlessOfOwnerOrRouting(int role)
    {
        var fixture = new TicketTestFixture();
        fixture.User.RoleIds = new[] { role };
        var visible = TicketVisibility.For(fixture.User).Compile();
        foreach (var ticket in fixture.Tickets.Items)
        {
            ticket.UserId = 999;
            foreach (var destination in new[] { 1, 2, 4, 5, 6 })
            foreach (var status in Enumerable.Range(1, 9))
            {
                ticket.CurrentRoleId = destination; ticket.StatusId = status;
                Assert.True(visible(ticket));
                Assert.True(TicketAccessPolicy.CanView(ticket, fixture.User));
            }
        }
    }

    [Fact]
    public void SharedViewing_DoesNotGrantRequesterMutationRightsOrAnonymousAccess()
    {
        var fixture = new TicketTestFixture(); fixture.User.RoleIds = new[] { 1 };
        var ticket = fixture.Technical;
        Assert.False(TicketAccessPolicy.CanEdit(ticket, fixture.User));
        Assert.False(TicketAccessPolicy.CanAssign(ticket, fixture.User));
        Assert.False(TicketAccessPolicy.CanChangeStatus(ticket, fixture.User, 8));
        fixture.User.IsAuthenticated = false;
        Assert.False(TicketAccessPolicy.CanView(ticket, fixture.User));
        Assert.False(TicketVisibility.For(fixture.User).Compile()(ticket));
    }
}
