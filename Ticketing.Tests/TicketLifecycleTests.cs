using Ticketing.Domain.Enums;
using Ticketing.EndPoints.Ticket.Command.ChangeStatus;
using Ticketing.EndPoints.Ticket.Command.ChangeGroup;
using Ticketing.EndPoints.Ticket.Command.ChangeDevelopedBy;
using Ticketing.EndPoints.Massage.Command.AddMassage;
using Ticketing.EndPoints.WorkFlow.Query.GetWorkFlow;
using Ticketing.EndPoints.Ticket.Query.GetGroupTicketList;
using Ticketing.EndPoints.Ticket.Query.GetGroupTicketInfo;
using Ticketing.EndPoints.Search.Query.GetSearchResult;
using Ticketing.Services;
using Ticketing.EndPoints.Dashboard.Query.GetDashboardChartData;
using Ticketing.EndPoints.Ticket.Command.AddTicket;
using Ticketing.EndPoints.Ticket.Command.EditeTicket;
using static Ticketing.Tests.TicketTestFixture;
using TicketDownload = Ticketing.EndPoints.Ticket.Query.DownloadFile;
using MessageDownload = Ticketing.EndPoints.Massage.Query.DownloadFile;

namespace Ticketing.Tests;

public class TicketLifecycleTests
{
    [Fact]
    public async Task History_UsesStoredPreviousRole_AndRejectsForeignRequester()
    {
        var f = new TicketTestFixture();
        var result = await f.Handler<GetWorkFlowHandler.Handler>().Handle(new GetWorkFlowQuery { TicketId = 2 }, default);
        var first = Json(result!).EnumerateArray().First();
        Assert.Equal(4, first.GetProperty("previousRoleId").GetInt32());
        Assert.Equal(5, first.GetProperty("currentRoleId").GetInt32());
        f.User.RoleIds = new[] { 1 };
        Assert.True(Denied(await f.Handler<GetWorkFlowHandler.Handler>().Handle(new GetWorkFlowQuery { TicketId = 2 }, default)));
    }

    [Fact]
    public async Task AnonymousHistory_IsDenied()
    {
        var f = new TicketTestFixture();
        f.User.IsAuthenticated = false;
        Assert.True(Denied(await f.Handler<GetWorkFlowHandler.Handler>().Handle(new GetWorkFlowQuery { TicketId = 2 }, default)));
    }

    [Fact]
    public async Task TechnicalInbox_AttachmentsFollowDetailAccess_AndOrphansAreDenied()
    {
        var f = new TicketTestFixture();
        Assert.NotNull(await f.Handler<TicketDownload.DownloadFile.Handler>().Handle(new TicketDownload.DownloadFileQuery { ticketId = 2 }, default));
        Assert.NotNull(await f.Handler<MessageDownload.DownloadMessageFile.Handler>().Handle(new MessageDownload.MassageDownloadFileQuery { massageId = 1 }, default));
        Assert.Null(await f.Handler<MessageDownload.DownloadMessageFile.Handler>().Handle(new MessageDownload.MassageDownloadFileQuery { massageId = 2 }, default));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task ForeignRequester_AndReadOnlySupervisor_CannotMutateTickets(int role)
    {
        var f = new TicketTestFixture();
        f.User.RoleIds = new[] { role };
        var result = await f.Handler<ChangeStatusHandler.Handler>().Handle(new ChangeStatusQuery { TicketId = 2, Status = 8, UserId = 999 }, default);
        Assert.True(Denied(result));
        Assert.Equal(3, f.Technical.StatusId);
        Assert.Equal(0, f.Tickets.Writes);
        Assert.Equal(0, f.Flows.Writes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(1)]
    public async Task Developer_CannotInventStatesOrBypassRequesterApproval(int status)
    {
        var f = new TicketTestFixture();
        Assert.True(Denied(await f.Handler<ChangeStatusHandler.Handler>().Handle(new ChangeStatusQuery { TicketId = 2, Status = status }, default)));
        Assert.Equal(3, f.Technical.StatusId);
        Assert.Equal(0, f.Tickets.Writes);
    }

    [Fact]
    public async Task Developer_ProgressAndCompletion_RecordActualRoleAndActor()
    {
        var f = new TicketTestFixture();
        f.Technical.StatusId = 7;
        await f.Handler<ChangeStatusHandler.Handler>().Handle(new ChangeStatusQuery { TicketId = 2, Status = 8, UserId = 999 }, default);
        await f.Handler<ChangeStatusHandler.Handler>().Handle(new ChangeStatusQuery { TicketId = 2, Status = 6, UserId = 999 }, default);
        Assert.Equal(6, f.Technical.StatusId);
        Assert.NotNull(f.Technical.ProcessEndDateTime);
        Assert.All(f.Flows.Items.Skip(1), flow => {
            Assert.Equal(42, flow.UserId);
            Assert.Equal(5, flow.CurrentRoleId);
            Assert.Equal(5, flow.PreviousRoleId);
            Assert.Equal("review-user", flow.Username);
        });
    }

    [Fact]
    public async Task Routing_OnlyAcceptsWorkflowDestinations_AndDeveloperCannotRouteForeignTicket()
    {
        var f = new TicketTestFixture();
        Assert.True(Denied(await f.Handler<ChangeRoleHandler.Handler>().Handle(new ChangeRoleQuery { TicketId = 2, RoleId = 999 }, default)));
        Assert.True(Denied(await f.Handler<ChangeRoleHandler.Handler>().Handle(new ChangeRoleQuery { TicketId = 3, RoleId = 5 }, default)));
        Assert.Equal(0, f.Tickets.Writes);
    }

    [Fact]
    public async Task RequesterManagerDeveloperLifecycle_PreservesHistoryAndOwnership()
    {
        var f = new TicketTestFixture();
        var ticket = f.Tickets.Items[0];
        f.User.UserId = 9; f.User.RoleIds = new[] { 4 };
        Assert.False(Denied(await f.Handler<ChangeRoleHandler.Handler>().Handle(new ChangeRoleQuery { TicketId = 1, RoleId = 5, UserId = 999 }, default)));
        f.User.UserId = 70; f.User.RoleIds = new[] { 2 };
        foreach (int state in new[] { 7, 8, 6 })
            Assert.False(Denied(await f.Handler<ChangeStatusHandler.Handler>().Handle(new ChangeStatusQuery { TicketId = 1, Status = state, UserId = 999 }, default)));
        f.User.UserId = 42; f.User.RoleIds = new[] { 1 };
        Assert.False(Denied(await f.Handler<ChangeStatusHandler.Handler>().Handle(new ChangeStatusQuery { TicketId = 1, Status = 1, UserId = 999 }, default)));
        Assert.Equal(1, ticket.StatusId);
        Assert.Equal(42, ticket.UserId);
        Assert.Equal(5, f.Flows.Writes);
        Assert.DoesNotContain(f.Flows.Items, flow => flow.UserId == 999);
    }

    [Fact]
    public async Task Replies_DenyForeignTicket_AndUseAuthenticatedActorEvenForAdmin()
    {
        var f = new TicketTestFixture();
        f.User.RoleIds = new[] { 1 };
        Assert.True(Denied(await f.Handler<AddMassageHandler.Handler>().Handle(new AddMassageQuery { TicketId = 2, Text = "unauthorized", UserId = 999, Username = "spoof" }, default)));
        Assert.Equal(0, f.Messages.Writes);
        f.User.RoleIds = new[] { 5 };
        await f.Handler<AddMassageHandler.Handler>().Handle(new AddMassageQuery { TicketId = 2, Text = "authorized", UserId = 999, Username = "spoof" }, default);
        Assert.Equal(42, f.Messages.Items.Last().UserId);
        Assert.Equal("review-user", f.Messages.Items.Last().Username);
    }

    [Fact]
    public async Task DeveloperAssignment_RejectsForeignTicketAndNegativeTime()
    {
        var f = new TicketTestFixture();
        Assert.True(Denied(await f.Handler<DevelopedByHandler.Handler>().Handle(new DevelopedByQuery { TicketId = 3, Time = "1", DeveloperId = Developer.p_rezayeh }, default)));
        Assert.True(Denied(await f.Handler<DevelopedByHandler.Handler>().Handle(new DevelopedByQuery { TicketId = 2, Time = "-1", DeveloperId = Developer.p_rezayeh }, default)));
        Assert.Equal(0, f.Tickets.Writes);
    }

    [Fact]
    public async Task ListAndCounters_IgnoreSpoofedIdentity_AndDoNotConfuseDeveloperEnumsWithUserIds()
    {
        var f = new TicketTestFixture();
        var list = await f.Handler<GetRoleTicketListHandler.Handler>().Handle(new GetRoleTicketListQuery { RoleId = 5, UserId = 8 }, default);
        Assert.Equal(new[] { 1, 2 }, Json(list!).EnumerateArray().Select(x => x.GetProperty("Id").GetInt32()).Order().ToArray());
        f.User.RoleIds = new[] { 1 };
        var counters = await f.Handler<GetRoleTicketInfoHandler.Handler>().Handle(new GetRoleTicketInfoQuery { RoleId = 4, UserId = 8, RequestTypeId = 1 }, default);
        Assert.Equal(1, Json(counters).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Search_NullOptionalFilters_AreSafe_AndTechnicalResultsAreScoped()
    {
        var f = new TicketTestFixture();
        var result = await f.Handler<GetSearchResultHandler.Handler>().Handle(new GetSearchResultQuery { TicketNumber = null!, Title = null!, Username = null! }, default);
        Assert.Equal(2, Json(result!).GetArrayLength());
    }

    [Fact]
    public async Task Search_EndDateIncludesFinalSecond_AndDoesNotMutateRequest()
    {
        var f = new TicketTestFixture();
        f.User.RoleIds = new[] { 5 };
        f.Tickets.Items[0].InsertDate = new DateTime(2026, 1, 2, 23, 59, 59);
        var query = new GetSearchResultQuery { InsertStartDateTime = new DateTime(2026, 1, 2), InsertEndDateTime = new DateTime(2026, 1, 2) };
        var result = await f.Handler<GetSearchResultHandler.Handler>().Handle(query, default);
        Assert.Equal(3, Json(result!).GetArrayLength());
        Assert.Equal(new DateTime(2026, 1, 2), query.InsertEndDateTime);
    }

    [Theory]
    [InlineData(2, 2, true)]
    [InlineData(3, 1, false)]
    public async Task TicketEditing_UsesWorkflowRoleRatherThanOwnershipAlone(int role, int ticketId, bool allowed)
    {
        var f = new TicketTestFixture();
        f.User.RoleIds = new[] { role };
        var result = await f.Handler<EditeTicketHandler.Handler>().Handle(new EditeTicketQuery {
            TicketId = ticketId, Title = "Updated", Text = "Description", ProjectId = 1,
            Priority = (Priority)1, RequestType = RequestType.Support, IsSchedule = IsSchedule.Support
        }, default);
        Assert.Equal(allowed, !Denied(result));
        Assert.Equal(allowed ? 1 : 0, f.Tickets.Writes);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(999, false)]
    public async Task TicketCreation_UsesCurrentActor_AndRequiresExistingProject(int project, bool allowed)
    {
        var f = new TicketTestFixture();
        f.User.RoleIds = new[] { 5 };
        var result = await f.Handler<AddTicketHandler.Handler>().Handle(new AddTicketQuery {
            Title = "Created ticket", Text = "Description", ProjectId = project, Priority = (Priority)1,
            RequestType = RequestType.Support, RoleId = 5, UserId = 999, Username = "spoof"
        }, default);
        Assert.Equal(allowed, !Denied(result));
        if (allowed) {
            Assert.Equal(42, f.Tickets.Items.Last().UserId);
            Assert.Equal("review-user", f.Tickets.Items.Last().Username);
            Assert.Equal(42, f.Flows.Items.Last().UserId);
        } else Assert.Equal(0, f.Tickets.Writes);
    }

    [Fact]
    public async Task RestoredDashboard_ToleratesHistoricalMissingTimes()
    {
        var f = new TicketTestFixture();
        f.Technical.StatusId = 6;
        f.Technical.ProcessEndDateTime = new DateTime(2026, 1, 2);
        f.Technical.TicketTime = "";
        f.Technical.DeveloperId = Developer.p_rezayeh;
        var response = await f.Handler<GetDashboardChartDataHandler.Handler>().Handle(new GetDashboardChartDataQuery { monthId = 10 }, default);
        Assert.NotNull(response);
        var data = System.Text.Json.JsonDocument.Parse((string)response).RootElement;
        Assert.Equal(0, data.GetProperty("DeveloperResultTime_support")[0].GetProperty("value").GetInt32());
    }

    [Fact]
    public async Task Editing_RejectsMissingProjectBeforeUpdating()
    {
        var f = new TicketTestFixture();
        var result = await f.Handler<EditeTicketHandler.Handler>().Handle(new EditeTicketQuery {
            TicketId = 2, Title = "Edit", Text = "Text", ProjectId = 999, Priority = (Priority)1,
            RequestType = RequestType.Support, IsSchedule = IsSchedule.Support
        }, default);
        Assert.True(Denied(result));
        Assert.Equal(0, f.Tickets.Writes);
    }

    [Fact]
    public async Task OrdinaryDashboard_RespectsRequesterVisibility()
    {
        var f = new TicketTestFixture();
        f.User.RoleIds = new[] { 1 };
        foreach (var ticket in f.Tickets.Items) {
            ticket.StatusId = 6; ticket.ProcessEndDateTime = DateTime.Now;
            ticket.TicketTime = ticket.UserId == 42 ? "10" : "20";
        }
        var response = await f.Handler<GetDashboardChartDataHandler.Handler>().Handle(new GetDashboardChartDataQuery { monthId = 1 }, default);
        var data = System.Text.Json.JsonDocument.Parse((string)response).RootElement;
        Assert.Equal(10, data.GetProperty("SupportResultYear").EnumerateArray().Sum(row => row.GetProperty("SupportTime").GetInt32()));
    }

    [Fact]
    public async Task Search_NotScheduledZero_IsAFilterRatherThanAll()
    {
        var f=new TicketTestFixture();
        f.User.RoleIds=new[]{5};
        f.Technical.IsSchedule=IsSchedule.no;
        var result=await f.Handler<GetSearchResultHandler.Handler>().Handle(new GetSearchResultQuery {IsSchadule="0"},default);
        Assert.Equal(1,Json(result).GetArrayLength());
    }
}
