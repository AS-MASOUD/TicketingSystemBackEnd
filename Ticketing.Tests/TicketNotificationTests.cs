using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Ticketing.Application.Service.Admin;
using Ticketing.Application.Service.Ticket;
using Ticketing.Application.Service.TicketFlow;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.EndPoints.Ticket.Command.AddTicket;
using Ticketing.EndPoints.Ticket.Command.ChangeStatus;
using Ticketing.EndPoints.Ticket.Command.ChangeGroup;
using Ticketing.EndPoints.Ticket.Command.ChangeDevelopedBy;
using Ticketing.Infrastructure.Database;
using Ticketing.Services;

namespace Ticketing.Tests;

[Collection("Restored database")]
public class TicketNotificationTests
{
    [Fact]
    public async Task NotificationWriteFailure_RollsBackTicketAndWorkflowTogether()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlServer("Data Source=.;Initial Catalog=Ticketing_Dev_Restored;Integrated Security=True;TrustServerCertificate=True;").Options;
        await using var db = new NotificationWriteFailureContext(options);
        var adminId = await db.UserRoles.Where(r => r.RoleId == 5 && db.Users.Any(u => u.Id == r.UserId && u.IsActive)).Select(r => r.UserId).FirstAsync();
        var actor = new TestCurrentUser { UserId = adminId, RoleIds = new[] { 5 } };
        var handler = new AddTicketHandler.Handler(new TicketService(db), new TestProjectsWithDefault(), new TicketFlowService(db), actor, NullLogger<AddTicketHandler>.Instance);
        var title = "notification_rollback_" + Guid.NewGuid().ToString("N");
        var command = new AddTicketQuery { Title = title, Text = "Rollback-only test", ProjectId = 1, Priority = (Priority)1, RequestType = RequestType.Support, RoleId = 5 };
        var behavior = new TicketNotificationBehavior<AddTicketQuery, object?>(db);
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(command, () => handler.Handle(command, default), default));
        Assert.Equal("Injected notification storage failure", failure.Message);
        var failedTicketId = db.ChangeTracker.Entries<Ticket>().Single().Entity.Id;
        await using var check = new TicketingDbContext(options);
        Assert.False(await check.Tickets.AnyAsync(t => t.Title == title));
        Assert.False(await check.TicketFlows.AnyAsync(f => f.TicketId == failedTicketId));
        Assert.False(await check.Notifications.AnyAsync(n => n.TargetUrl == $"/ticket/detail?id={failedTicketId}"));
    }

    [Fact]
    public async Task TicketEvents_CreateRecipientNotificationsAndUnreadCounts_WithoutLeakingToOtherUsers()
    {
        await using var db = new TicketingDbContext(new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlServer("Data Source=.;Initial Catalog=Ticketing_Dev_Restored;Integrated Security=True;TrustServerCertificate=True;").Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        async Task<TicketingUser> User(int role, string? name = null, string? family = null)
        {
            var u = new TicketingUser { Username = "notif_test_" + Guid.NewGuid().ToString("N"), DefaultRoleId = role, Name = name, Family = family };
            db.Users.Add(u); await db.SaveChangesAsync();
            db.UserRoles.Add(new TicketingUserRole { UserId = u.Id, RoleId = role, RoleName = "test" });
            await db.SaveChangesAsync(); return u;
        }
        var owner = await User(1); var manager = await User(5); var technical = await User(4);
        var outsider = await User(1); var developer = await User(2, "امیرمسعود", "صالحی");
        var inactive = await User(4); inactive.IsActive = false; await db.SaveChangesAsync();
        var actor = new TestCurrentUser { UserId = owner.Id, RoleIds = new[] { 1 } };
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton(db);
        services.AddSingleton<ICurrentUserService>(actor);
        services.AddScoped<ITicketService, TicketService>(); services.AddScoped<ITicketFlowService, TicketFlowService>();
        services.AddSingleton<IProjectService>(new TestProjectsWithDefault());
        services.AddSingleton<IProjectRoleService>(new TestProjectRoles());
        services.AddMediatR(configuration => {
            var assembly = typeof(AddTicketQuery).Assembly;
            configuration.RegisterServicesFromAssembly(assembly);
            foreach (var behavior in assembly.GetTypes().Where(t => t.IsGenericTypeDefinition && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))))
                configuration.AddOpenBehavior(behavior);
        });
        await using var provider = services.BuildServiceProvider(); var mediator = provider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AddTicketQuery { Title = "Notification lifecycle", Text = "Rollback-only integration test", ProjectId = 1, Priority = (Priority)1, RequestType = RequestType.Support });
        var ticket = Assert.IsType<Ticket>(result); var target = $"/ticket/detail?id={ticket.Id}";
        async Task<List<TicketingNotification>> Notifications() => await db.Notifications.AsNoTracking().Where(n => n.TargetUrl == target).ToListAsync();
        var created = await Notifications();
        Assert.Contains(created, n => n.UserId == owner.Id && n.Type == "ticket-created");
        Assert.Contains(created, n => n.UserId == technical.Id && n.Type == "ticket-created");
        Assert.Contains(created, n => n.UserId == manager.Id && n.Type == "ticket-created");
        Assert.DoesNotContain(created, n => n.UserId == outsider.Id || n.UserId == inactive.Id);
        actor.UserId = manager.Id; actor.RoleIds = new[] { 5 };
        await mediator.Send(new ChangeStatusQuery { TicketId = ticket.Id, Status = 8 });
        Assert.Contains(await Notifications(), n => n.UserId == owner.Id && n.Type == "ticket-status" && n.Message.Contains("در حال انجام"));
        await mediator.Send(new ChangeRoleQuery { TicketId = ticket.Id, RoleId = 5 });
        Assert.Contains(await Notifications(), n => n.UserId == developer.Id && n.Type == "ticket-assigned");
        var beforeNoop = (await Notifications()).Count;
        await mediator.Send(new ChangeRoleQuery { TicketId = ticket.Id, RoleId = 5 });
        Assert.Equal(beforeNoop, (await Notifications()).Count);
        var beforeDeveloper = (await Notifications()).Max(n => n.Id);
        await mediator.Send(new DevelopedByQuery { TicketId = ticket.Id, DeveloperId = (Developer)5, Time = "30" });
        Assert.Contains(await Notifications(), n => n.Id > beforeDeveloper && n.UserId == developer.Id && n.Type == "ticket-assigned");
        Assert.DoesNotContain(await Notifications(), n => n.UserId == outsider.Id || n.UserId == inactive.Id);
        var notifications = new NotificationService(db); var unread = await notifications.GetUnreadCountAsync(owner.Id);
        Assert.True(unread >= 3);
        var first = (await Notifications()).First(n => n.UserId == owner.Id);
        Assert.False(await notifications.MarkAsReadAsync(first.Id, outsider.Id));
        Assert.True(await notifications.MarkAsReadAsync(first.Id, owner.Id));
        Assert.Equal(unread - 1, await notifications.GetUnreadCountAsync(owner.Id));
        await notifications.MarkAllAsReadAsync(owner.Id);
        Assert.Equal(0, await notifications.GetUnreadCountAsync(owner.Id));
        actor.UserId = outsider.Id; actor.RoleIds = new[] { 1 };
        var beforeDenied = (await Notifications()).Count;
        Assert.True(TicketTestFixture.Denied(await mediator.Send(new ChangeStatusQuery { TicketId = ticket.Id, Status = 4 })));
        Assert.Equal(beforeDenied, (await Notifications()).Count);
    }

    private sealed class TestProjectsWithDefault : MemoryRepository<Project>, IProjectService
    { public TestProjectsWithDefault() { Items.Add(new Project { Id = 1, Name = "Test project" }); } }
    private sealed class NotificationWriteFailureContext(DbContextOptions<TicketingDbContext> options) : TicketingDbContext(options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ChangeTracker.Entries<TicketingNotification>().Any(n => n.State == EntityState.Added))
                throw new InvalidOperationException("Injected notification storage failure");
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
