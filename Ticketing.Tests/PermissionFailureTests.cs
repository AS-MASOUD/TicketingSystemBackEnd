using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Service.Admin;
using Ticketing.Infrastructure.Database;
using Xunit;

namespace Ticketing.Tests;

public class PermissionFailureTests
{
    [Fact]
    public async Task HasPermissionAsync_WhenDatabaseLookupFails_ShouldDenyPermission()
    {
        // Construct TicketingDbContext with options lacking a provider so query throws without touching any database
        var options = new DbContextOptionsBuilder<TicketingDbContext>().Options;
        await using var dbContext = new TicketingDbContext(options);
        var service = new PermissionService(dbContext);

        var result = await service.HasPermissionAsync(userId: 1, permissionCode: "tickets.view");

        Assert.False(result);
    }

    [Theory]
    [InlineData("roles")]
    [InlineData("permissions")]
    public async Task UnavailableMetadata_DoesNotReturnInventedDatabaseRows(string resource)
    {
        await using var db = new TicketingDbContext(new DbContextOptionsBuilder<TicketingDbContext>().Options);
        var service = new PermissionService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            if (resource == "roles") await service.GetAllRolesAsync();
            else await service.GetAllPermissionsAsync();
        });
    }
}
