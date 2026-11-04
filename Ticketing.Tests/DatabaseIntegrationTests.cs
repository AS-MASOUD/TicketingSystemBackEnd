using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Service.User;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;
using Xunit;

namespace Ticketing.Tests;

public class DatabaseIntegrationTests
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
    public async Task RestoredDatabase_ShouldContainHistoricalTicketsAndMessages()
    {
        using var context = CreateDbContext();

        // Act
        int ticketCount = await context.Tickets.CountAsync();
        int messageCount = await context.Massages.CountAsync();
        int projectCount = await context.Projects.CountAsync();
        int statusCount = await context.Statuses.CountAsync();

        // Assert
        Assert.Equal(2433, ticketCount);
        Assert.Equal(3606, messageCount);
        Assert.Equal(8, projectCount);
        Assert.Equal(9, statusCount);
    }

    [Fact]
    public async Task UserService_EnsureSeedUsers_ShouldProvisionAdminAndHistoricalUsers()
    {
        using var context = CreateDbContext();
        var userService = new UserService(context);

        // Act
        await userService.EnsureSeedUsersAsync();

        var adminVira = await userService.GetByUsernameWithRolesAsync("adminvira");
        var adminTaz = await userService.GetByUsernameWithRolesAsync("admintaz");
        var regularUser = await userService.GetByUsernameWithRolesAsync("mrnadaf");

        // Assert
        Assert.NotNull(adminVira);
        Assert.Equal(3, adminVira.Id); // Preserved historical UserId
        Assert.Equal(5, adminVira.DefaultRoleId);
        Assert.True(adminVira.IsActive);
        Assert.Contains(adminVira.UserRoles, r => r.RoleId == 5);

        Assert.NotNull(adminTaz);
        Assert.Equal(2, adminTaz.Id); // Preserved historical UserId
        Assert.Equal(4, adminTaz.DefaultRoleId);
        Assert.Contains(adminTaz.UserRoles, r => r.RoleId == 4);

        Assert.NotNull(regularUser);
        Assert.Equal(14, regularUser.Id); // Preserved historical UserId
        Assert.Equal(2, regularUser.DefaultRoleId);
        Assert.Contains(regularUser.UserRoles, r => r.RoleId == 2);
    }

    [Fact]
    public async Task UserService_VerifyPassword_ShouldValidateCorrectCredentials()
    {
        using var context = CreateDbContext();
        var userService = new UserService(context);
        await userService.EnsureSeedUsersAsync();

        var adminVira = await userService.GetByUsernameWithRolesAsync("adminvira");
        Assert.NotNull(adminVira);

        // Act & Assert
        bool isValid = await userService.VerifyPasswordAsync(adminVira, "Admin@Vira#2026");
        bool isInvalid = await userService.VerifyPasswordAsync(adminVira, "WrongPassword");

        Assert.True(isValid);
        Assert.False(isInvalid);
    }

    [Fact]
    public async Task TicketQuery_ShouldPreserveRelationshipsWithoutDataLoss()
    {
        using var context = CreateDbContext();

        // Act - Id 1 was archived in historical data, so get earliest existing ticket
        var sampleTicket = await context.Tickets.OrderBy(t => t.Id).FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(sampleTicket);
        Assert.NotNull(sampleTicket.Title);
        Assert.True(sampleTicket.StatusId >= 1 && sampleTicket.StatusId <= 9);
        Assert.True(sampleTicket.ProjectId >= 1);
    }
}
