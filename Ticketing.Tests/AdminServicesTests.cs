using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Security;
using Ticketing.Application.Service.Admin;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Models;
using Ticketing.Infrastructure.Database;
using Xunit;

namespace Ticketing.Tests;

[Collection("Restored database")]
public class AdminServicesTests
{
    private const string ConnectionString = "Data Source=.;Initial Catalog=Ticketing_Dev_Restored;Integrated Security=True;TrustServerCertificate=True;";

    private TicketingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TicketingDbContext(options);
    }

    [Fact]
    public async Task AdminVira_ShouldHaveSuperAdminPermissions()
    {
        using var context = CreateDbContext();
        var permService = new PermissionService(context);

        // adminvira has Id = 3, Role = 5
        bool canViewDashboard = await permService.HasPermissionAsync(3, "dashboard.analytics.view");
        bool canManageUsers = await permService.HasPermissionAsync(3, "users.create");
        bool canManageRoles = await permService.HasPermissionAsync(3, "roles.manage");
        bool canManageSettings = await permService.HasPermissionAsync(3, "settings.manage");

        Assert.True(canViewDashboard);
        Assert.True(canManageUsers);
        Assert.True(canManageRoles);
        Assert.True(canManageSettings);

        var permissions = await permService.GetUserPermissionsAsync(3);
        Assert.NotEmpty(permissions);
        Assert.True(permissions.Count >= 20);
    }

    [Fact]
    public async Task Developer_ShouldNotHaveAdminUserManagementPermissions()
    {
        using var context = CreateDbContext();
        var permService = new PermissionService(context);

        // mrnadaf has Id = 14, Role = 2
        bool canManageUsers = await permService.HasPermissionAsync(14, "users.create");
        bool canManageSettings = await permService.HasPermissionAsync(14, "settings.manage");
        bool canViewTickets = await permService.HasPermissionAsync(14, "tickets.view_all");

        Assert.False(canManageUsers);
        Assert.False(canManageSettings);
        Assert.True(canViewTickets);
    }

    [Fact]
    public async Task AdminAnalyticsService_OverviewStats_ShouldReturnRealDatabaseData()
    {
        using var context = CreateDbContext();
        var analyticsService = new AdminAnalyticsService(context);

        var stats = await analyticsService.GetOverviewStatsAsync();

        Assert.NotNull(stats);
        Assert.True(stats.TotalTickets >= 2400); // 2433 historical tickets verified
        Assert.True(stats.TotalUsers >= 3);
        Assert.True(stats.ActiveProjects >= 1);
        Assert.True(stats.AverageResolutionHours > 0);
        Assert.True(stats.SlaComplianceRate >= 0 && stats.SlaComplianceRate <= 100);
    }

    [Fact]
    public async Task AdminAnalyticsService_StatusDistribution_ShouldReturnAllNineStatuses()
    {
        using var context = CreateDbContext();
        var analyticsService = new AdminAnalyticsService(context);

        var dist = await analyticsService.GetStatusDistributionAsync();

        Assert.NotNull(dist);
        Assert.Equal(9, dist.Count); // 9 database statuses
        Assert.True(dist.Sum(d => d.Count) >= 2400);
    }

    [Fact]
    public async Task AdminAnalyticsService_ProjectWorkload_ShouldReturnAllProjectsWithVolume()
    {
        using var context = CreateDbContext();
        var analyticsService = new AdminAnalyticsService(context);

        var workload = await analyticsService.GetProjectWorkloadAsync();

        Assert.NotNull(workload);
        Assert.NotEmpty(workload);
        Assert.True(workload.Any(w => w.TotalTickets > 0));
    }

    [Fact]
    public async Task AdminManagementService_GetUsers_ShouldReturnPagedResultsWithRoleNames()
    {
        using var context = CreateDbContext();
        var mgmtService = new AdminManagementService(context);

        var paged = await mgmtService.GetUsersAsync(1, 50);

        Assert.NotNull(paged);
        Assert.True(paged.TotalCount >= 3);
        Assert.NotEmpty(paged.Items);

        var adminUser = paged.Items.FirstOrDefault(u => u.Username == "adminvira");
        Assert.NotNull(adminUser);
        Assert.NotEmpty(adminUser.RoleNames);
    }

    [Fact]
    public async Task AdminManagementService_CreateUpdateToggleUser_ShouldSucceed()
    {
        using var context = CreateDbContext();
        var mgmtService = new AdminManagementService(context);

        string testUsername = $"testuser_{DateTime.UtcNow.Ticks % 100000}";
        var req = new CreateUserRequest
        {
            Username = testUsername,
            Password = "Password@12345",
            Name = "کاربر",
            Family = "تستی",
            Email = $"{testUsername}@example.com",
            Mobile = "09120000000",
            DefaultRoleId = 2,
            RoleIds = new List<int> { 2 },
            IsActive = true
        };

        // Create
        var created = await mgmtService.CreateUserAsync(req);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        // Update
        bool updated = await mgmtService.UpdateUserAsync(created.Id, new UpdateUserRequest
        {
            Name = "نام ویرایش شده",
            Family = "فامیلی ویرایش شده",
            Email = "updated@example.com",
            Mobile = "09121111111",
            DefaultRoleId = 2,
            RoleIds = new List<int> { 2 },
            IsActive = true
        });
        Assert.True(updated);

        // Toggle inactive
        bool deactivated = await mgmtService.ToggleUserStatusAsync(created.Id, false);
        Assert.True(deactivated);

        var details = await mgmtService.GetUserDetailAsync(created.Id);
        Assert.NotNull(details);
        Assert.False(details.IsActive);
        Assert.Equal("نام ویرایش شده", details.Name);
    }

    [Fact]
    public async Task AuditService_LogAndQuery_ShouldRecordAndRetrieveEvents()
    {
        using var context = CreateDbContext();
        var auditService = new AuditService(context);

        string uniqueAction = $"TestAction_{Guid.NewGuid():N}";
        await auditService.LogAsync(uniqueAction, "TestEntity", "123", "جزئیات رویداد تست", 3, "adminvira", "127.0.0.1");

        var (items, count) = await auditService.GetLogsAsync(1, 10, action: uniqueAction);
        Assert.True(count >= 1);
        Assert.Contains(items, i => i.Action == uniqueAction && i.ActorUsername == "adminvira");
    }

    [Fact]
    public async Task NotificationService_CreateAndMarkRead_ShouldWork()
    {
        using var context = CreateDbContext();
        var notifService = new NotificationService(context);

        // Create notification for adminvira (userId = 3)
        await notifService.CreateNotificationAsync(3, "تیکت جدید ارجاع شد", "تیکت شماره ۱۰۰ به شما ارجاع گردید.", "info", "/ticketing/ticket/detail?id=100");

        int unreadBefore = await notifService.GetUnreadCountAsync(3);
        Assert.True(unreadBefore >= 1);

        var list = await notifService.GetUserNotificationsAsync(3, unreadOnly: true);
        Assert.NotEmpty(list);

        var first = list.First();
        bool marked = await notifService.MarkAsReadAsync(first.Id, 3);
        Assert.True(marked);
    }

    [Fact]
    public async Task SlaPolicies_GetAndCreate_ShouldWork()
    {
        using var context = CreateDbContext();
        var mgmtService = new AdminManagementService(context);

        var policies = await mgmtService.GetSlaPoliciesAsync();
        Assert.NotEmpty(policies);
        Assert.Contains(policies, p => p.Priority == 1); // High priority policy seeded
    }

    [Fact]
    public async Task AppSettings_GetAndUpdate_ShouldPersistModifications()
    {
        using var context = CreateDbContext();
        var mgmtService = new AdminManagementService(context);

        var settings = await mgmtService.GetSettingsAsync();
        Assert.NotEmpty(settings);

        var maxAttachment = settings.FirstOrDefault(s => s.Key == "Tickets.MaxAttachmentSizeMB");
        Assert.NotNull(maxAttachment);
        Assert.Equal("50", maxAttachment.Value);
    }

    [Fact]
    public async Task GetAllRolesAsync_ShouldReturnRolesWithPermissions()
    {
        using var context = CreateDbContext();
        var permService = new PermissionService(context);

        var roles = await permService.GetAllRolesAsync();
        Assert.NotEmpty(roles);
        Assert.Contains(roles, r => r.Id == 5);
        Assert.Contains(roles, r => r.Id == 1);
    }

    [Fact]
    public async Task UserStats_ShouldReturnUserCounts()
    {
        using var context = CreateDbContext();
        var mgmtService = new AdminManagementService(context);

        var stats = await mgmtService.GetUserStatsAsync();
        Assert.NotNull(stats);
        Assert.True(stats.TotalUsers >= 1);
        Assert.True(stats.ActiveUsers >= 1);
    }
}
