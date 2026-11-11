using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ticketing.Domain.Common;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Services;
using TicketEntity = Ticketing.Domain.Entities.Ticket;
using StatusEntity = Ticketing.Domain.Entities.Status;

namespace Ticketing.Tests;

internal class MemoryRepository<T> : IAsyncRepository<T> where T : BaseEntity<int>
{
    public List<T> Items { get; } = new();
    public int Writes { get; private set; }
    public Task<T> AddAsync(T entity) { Writes++; if (entity.Id == 0) entity.Id = Items.Count + 1; Items.Add(entity); return Task.FromResult(entity); }
    public Task<T> UpdateAsync(T entity) { Writes++; return Task.FromResult(entity); }
    public Task<bool> DeleteAsync(T entity) { Writes++; return Task.FromResult(Items.Remove(entity)); }
    public Task<T> GetAsync(Expression<Func<T, bool>> expression) => Task.FromResult(Items.FirstOrDefault(expression.Compile())!);
    public Task<List<T>> ListAsync(Expression<Func<T, bool>>? expression) => Task.FromResult(expression == null ? Items.ToList() : Items.Where(expression.Compile()).ToList());
}
internal sealed class TestTickets : MemoryRepository<TicketEntity>, ITicketService
{
    public Task<int> GetNextRowNumberAsync() =>
        Task.FromResult(Items.Select(t => t.TicketRowNumber ?? 0).DefaultIfEmpty(0).Max() + 1);
}
internal sealed class TestFlows : MemoryRepository<TicketFlow>, ITicketFlowService { }
internal sealed class TestProjects : MemoryRepository<Project>, IProjectService { }
internal sealed class TestProjectRoles : MemoryRepository<ProjectRole>, IProjectRoleService { }
internal sealed class TestStatuses : MemoryRepository<StatusEntity>, IStatusService { }
internal sealed class TestMessages : MemoryRepository<Massage>, IMassageService { }

internal sealed class TestCurrentUser : ICurrentUserService
{
    public int? UserId { get; set; } = 42;
    public string? Username => "review-user";
    public IReadOnlyList<int> RoleIds { get; set; } = new[] { 2 };
    public int? DefaultRoleId => RoleIds.FirstOrDefault();
    public bool IsAuthenticated { get; set; } = true;
    public bool IsAdminVira => HasRole(5);
    public bool IsAdminTaz => HasRole(4);
    public bool HasRole(int id) => RoleIds.Contains(id);
}

internal sealed class TicketTestFixture
{
    public TestTickets Tickets { get; } = new();
    public TestFlows Flows { get; } = new();
    public TestProjects Projects { get; } = new();
    public TestProjectRoles ProjectRoles { get; } = new();
    public TestStatuses Statuses { get; } = new();
    public TestMessages Messages { get; } = new();
    public TestCurrentUser User { get; } = new();
    public TicketEntity Technical => Tickets.Items[1];
    public TicketTestFixture()
    {
        Tickets.Items.AddRange(new[] {
            Ticket(1, 42, 4, 2),
            Ticket(2, 7, 5, 3),
            Ticket(3, 8, 4, 2)
        });
        // Legacy developer enum values are not login user IDs. This collision must not grant visibility.
        Tickets.Items[2].DeveloperId = (Developer)42;
        Projects.Items.Add(new Project { Id = 1, Name = "Test project" });
        Statuses.Items.AddRange(Enumerable.Range(1, 9).Select(id => new StatusEntity { Id = id, Name = $"Status {id}" }));
        Flows.Items.Add(new TicketFlow { Id = 1, TicketId = 2, PreviousRoleId = 4, CurrentRoleId = 5, StatusId = 3, UserId = 9, Username = "manager", InsertDate = DateTime.Now });
        Messages.Items.Add(new Massage { Id = 1, TicketId = 2, FilePath = "test.pdf", Text = "reply", Username = "manager" });
        Messages.Items.Add(new Massage { Id = 2, TicketId = 999, FilePath = "orphan.pdf", Text = "orphan", Username = "manager" });
    }
    private static TicketEntity Ticket(int id, int owner, int role, int status) => new() {
        Id = id, UserId = owner, CurrentRoleId = role, InsertedRoleId = 1, StatusId = status,
        TicketNumber = $"14040101000{id}", TicketRowNumber = id, Title = $"Ticket {id}", Text = "Description",
        Username = $"owner-{owner}", FilePath = "test.pdf", ProjectId = 1, Priority = (Priority)1,
        RequestTypeId = RequestType.Support, IsSchedule = IsSchedule.Support,
        InsertDate = new DateTime(2026, 1, 2, 12, 0, 0), DeveloperId = Developer.unknown
    };
    public T Handler<T>() where T : class
    {
        object[] services = { Tickets, Flows, Projects, ProjectRoles, Statuses, Messages, User, new HttpContextAccessor() };
        var ctor = typeof(T).GetConstructors().Single();
        return (T)ctor.Invoke(ctor.GetParameters().Select(p => {
            if (p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(ILogger<>))
                return Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(p.ParameterType.GenericTypeArguments))!;
            return services.First(s => p.ParameterType.IsInstanceOfType(s));
        }).ToArray());
    }
    public static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);
    public static bool Denied(object? value) => value == null || value is IStatusCodeHttpResult result && result.StatusCode >= 400;
}
