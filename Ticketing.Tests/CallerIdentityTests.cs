using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.EndPoints.Group.Query.GetGroup;
using Ticketing.EndPoints.Massage.Command.AddMassage;
using Ticketing.EndPoints.Search.Query.GetSearchResult;
using Ticketing.EndPoints.Ticket.Command.AddTicket;
using Ticketing.EndPoints.Ticket.Command.ChangeGroup;
using Ticketing.EndPoints.Ticket.Command.ChangeStatus;
using Ticketing.EndPoints.Ticket.Query.GetGroupTicketInfo;
using Ticketing.EndPoints.Ticket.Query.GetGroupTicketList;
using Ticketing.EndPoints.Ticket.Query.GetYearTickerInfo;
using Ticketing.Services;

namespace Ticketing.Tests;

public class CallerIdentityTests
{
    private static TestCurrentUser Employer() => new() { RoleIds = new[] { 1 } };

    private static Task<object> Run<T>(T request, TestCurrentUser user) where T : notnull =>
        new CallerIdentityBehavior<T, object>(user).Handle(request, () => Task.FromResult<object>("allowed"), default);

    [Fact]
    public async Task ForgedUserId_IsRejectedAcrossLegacyQueriesAndCommands()
    {
        await Denied(new AddTicketQuery { UserId = 99 });
        await Denied(new AddMassageQuery { UserId = 99 });
        await Denied(new ChangeStatusQuery { UserId = 99 });
        await Denied(new ChangeRoleQuery { UserId = 99, RoleId = 5 });
        await Denied(new GetRoleTicketListQuery { UserId = 99 });
        await Denied(new GetRoleTicketInfoQuery { UserId = 99 });
        await Denied(new GetYearTickerInfoQuery { UserId = 99 });
    }

    [Fact]
    public async Task ForgedRoleId_IsRejectedAcrossAllCallerRoleFields()
    {
        await Denied(new AddTicketQuery { RoleId = 5 });
        await Denied(new GetRoleTicketListQuery { RoleId = 5 });
        await Denied(new GetRoleTicketInfoQuery { RoleId = 5 });
        await Denied(new GetYearTickerInfoQuery { RoleId = 5 });
        await Denied(new GetProjectListQuery { RoleId = 5 });
    }

    private static async Task Denied<T>(T request) where T : notnull
    {
        var exception = await Assert.ThrowsAsync<CallerIdentityException>(() => Run(request, Employer()));
        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task MissingIdentity_IsFilledFromTheAuthenticatedSession()
    {
        var request = new AddTicketQuery();
        await Run(request, Employer());
        Assert.Equal(42, request.UserId);
        Assert.Equal(1, request.RoleId);
        Assert.Equal("review-user", request.Username);
        var optional = new GetRoleTicketListQuery();
        await Run(optional, Employer());
        Assert.Equal(42, optional.UserId);
        Assert.Equal(1, optional.RoleId);
    }

    [Fact]
    public async Task ForgedUsername_IsRejectedButSearchUsernameRemainsAFilter()
    {
        await Denied(new AddTicketQuery { Username = "adminvira" });
        await Denied(new AddMassageQuery { Username = "adminvira" });
        var search = new GetSearchResultQuery { Username = "another-user" };
        await Run(search, Employer());
        Assert.Equal("another-user", search.Username);
    }

    [Fact]
    public async Task DestinationRole_IsNotMistakenForTheCallersRole()
    {
        var request = new ChangeRoleQuery { RoleId = 5 };
        await Run(request, Employer());
        Assert.Equal(5, request.RoleId);
        Assert.Equal(42, request.UserId);
        // The handler still applies TicketAccessPolicy.CanRoute to the destination.
    }

    [Fact]
    public async Task AssignedSecondaryRole_IsAccepted_RevokedRoleIsRejected()
    {
        var user = Employer();
        user.RoleIds = new[] { 1, 6 };
        await Run(new GetProjectListQuery { RoleId = 6 }, user);
        user.RoleIds = new[] { 1 };
        var error = await Assert.ThrowsAsync<CallerIdentityException>(() => Run(new GetProjectListQuery { RoleId = 6 }, user));
        Assert.Equal(403, error.StatusCode);
    }

    [Theory]
    [InlineData(false, 42, true)]
    [InlineData(true, 0, true)]
    [InlineData(true, 42, false)]
    public async Task InvalidSession_IsRejectedEvenWithoutCallerFields(bool authenticated, int userId, bool hasRoles)
    {
        var user = new TestCurrentUser { IsAuthenticated = authenticated, UserId = userId,
            RoleIds = hasRoles ? new[] { 1 } : Array.Empty<int>() };
        var error = await Assert.ThrowsAsync<CallerIdentityException>(() => Run(new GetSearchResultQuery(), user));
        Assert.Equal(401, error.StatusCode);
    }

    [Fact]
    public async Task Mediator_BlocksTamperingBeforeTheHandlerExecutes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentUserService>(Employer());
        services.AddMediatR(configuration => {
            configuration.RegisterServicesFromAssemblyContaining<IdentityProbeHandler>();
            configuration.AddOpenBehavior(typeof(CallerIdentityBehavior<,>));
        });
        await using var provider = services.BuildServiceProvider();
        var request = new IdentityProbe { UserId = 99 };
        await Assert.ThrowsAsync<CallerIdentityException>(() => provider.GetRequiredService<IMediator>().Send(request));
        Assert.False(request.Executed);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    public async Task Middleware_ReturnsJsonStatusWithoutRedirecting(int status)
    {
        var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        await using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = body;
        var middleware = new CallerIdentityExceptionMiddleware(_ => throw new CallerIdentityException(status));
        await middleware.InvokeAsync(context);
        Assert.Equal(status, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
        Assert.True(body.Length > 0);
    }
}

public sealed class IdentityProbe : IRequest<object>
{
    [CallerIdentity(CallerIdentityKind.UserId)] public int UserId { get; set; }
    public bool Executed { get; set; }
}

public sealed class IdentityProbeHandler : IRequestHandler<IdentityProbe, object>
{
    public Task<object> Handle(IdentityProbe request, CancellationToken cancellationToken)
    {
        request.Executed = true;
        return Task.FromResult<object>("allowed");
    }
}
