using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Service.Admin;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Tests;

[Collection("Restored database")]
public class RealDataAndPermissionTests
{
    private static TicketingDbContext Context() => new(new DbContextOptionsBuilder<TicketingDbContext>()
        .UseSqlServer("Data Source=.;Initial Catalog=Ticketing_Dev_Restored;Integrated Security=True;TrustServerCertificate=True;").Options);

    [Fact]
    public async Task EmptyAnalytics_DoesNotInventResolutionOrResponseHours()
    {
        await using var db = Context();
        var stats = await new AdminAnalyticsService(db).GetOverviewStatsAsync(projectId: int.MaxValue);
        Assert.Equal(0, stats.TotalTickets);
        Assert.Equal(0, stats.AverageResolutionHours);
        Assert.Equal(0, stats.AverageFirstResponseHours);
    }

    [Theory]
    [InlineData("users.view", "users.manage", false)]
    [InlineData("users.view", "users.view", true)]
    [InlineData("users.manage", "users.view", true)]
    [InlineData("users.manage", "users.create", true)]
    public async Task PermissionGrants_RespectViewAndManagementScopes(string grant, string requested, bool allowed)
    {
        await using var db = Context();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var role = new TicketingRole { Name = "review_" + Guid.NewGuid().ToString("N"), DisplayName = "Read only", IsActive = true };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        var user = new TicketingUser { Username = "review_" + Guid.NewGuid().ToString("N"), DefaultRoleId = role.Id, IsActive = true };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Code == grant);
        if (permission == null) {
            permission = new TicketingPermission { Code = grant, Name = grant, Module = "Users" };
            db.Permissions.Add(permission);
            await db.SaveChangesAsync();
        }
        db.UserRoles.Add(new TicketingUserRole { UserId = user.Id, RoleId = role.Id, RoleName = role.Name });
        db.RolePermissions.Add(new TicketingRolePermission { RoleId = role.Id, PermissionId = permission.Id });
        await db.SaveChangesAsync();
        var service = new PermissionService(db);
        Assert.True(await service.HasPermissionAsync(user.Id, grant));
        Assert.Equal(allowed, await service.HasPermissionAsync(user.Id, requested));
        // Disposal rolls back the isolated local transaction; no test users or roles are retained.
    }

    [Theory]
    [InlineData("disable")]
    [InlineData("demote")]
    [InlineData("remove")]
    public async Task LastActiveAdministrator_CannotBeRemovedByAnyManagementPath(string operation)
    {
        await using var db = Context();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var admins = await db.Users.Include(u => u.UserRoles)
            .Where(u => u.IsActive && u.UserRoles.Any(r => r.RoleId == 5)).ToListAsync();
        Assert.NotEmpty(admins);
        var target = admins[0];
        foreach (var other in admins.Skip(1)) other.IsActive = false;
        if (operation == "remove") target.DefaultRoleId = 2;
        await db.SaveChangesAsync();
        var service = new AdminManagementService(db);
        await Assert.ThrowsAnyAsync<InvalidOperationException>(async () => {
            if (operation == "disable") await service.ToggleUserStatusAsync(target.Id, false);
            else if (operation == "remove") await service.RemoveUserRoleAsync(target.Id, 5);
            else await service.UpdateUserAsync(target.Id, new Ticketing.Domain.Models.UpdateUserRequest {
                IsActive = true, DefaultRoleId = 2, RoleIds = new() { 2 }, ProjectIds = new() { 1 }
            });
        });
    }

    [Fact]
    public async Task UserCreation_RejectsUnknownRoleBeforeSavingUser()
    {
        await using var db = Context();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var name = "review_invalid_" + Guid.NewGuid().ToString("N");
        await Assert.ThrowsAnyAsync<InvalidOperationException>(()=>new AdminManagementService(db).CreateUserAsync(new Ticketing.Domain.Models.CreateUserRequest {
            Username=name, Password="Test@Password#123", DefaultRoleId=999999, RoleIds=new(){999999}, IsActive=true
        }));
        Assert.False(await db.Users.AnyAsync(u=>u.Username==name));
    }
}
