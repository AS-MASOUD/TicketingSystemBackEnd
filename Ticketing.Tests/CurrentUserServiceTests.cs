using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Ticketing.Services;
using Xunit;

namespace Ticketing.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public void WhenUserIsAnonymous_ShouldReportNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CurrentUserService(accessor);

        // Assert
        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
        Assert.Null(service.Username);
        Assert.Empty(service.RoleIds);
        Assert.False(service.IsAdminVira);
        Assert.False(service.IsAdminTaz);
    }

    [Fact]
    public void WhenUserIsAdminVira_ShouldCorrectlyIdentifyRoleAndId()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "3"),
            new Claim(ClaimTypes.Name, "adminvira"),
            new Claim(ClaimTypes.Role, "5"),
            new Claim("DefaultRole", "5")
        };
        var identity = new ClaimsIdentity(claims, "Cookie");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CurrentUserService(accessor);

        // Assert
        Assert.True(service.IsAuthenticated);
        Assert.Equal(3, service.UserId);
        Assert.Equal("adminvira", service.Username);
        Assert.Equal(5, service.DefaultRoleId);
        Assert.True(service.IsAdminVira);
        Assert.False(service.IsAdminTaz);
        Assert.True(service.HasRole(5));
        Assert.False(service.HasRole(4));
    }

    [Fact]
    public void WhenUserIsAdminTaz_ShouldCorrectlyIdentifyRoleAndId()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Name, "admintaz"),
            new Claim(ClaimTypes.Role, "4"),
            new Claim("DefaultRole", "4")
        };
        var identity = new ClaimsIdentity(claims, "Cookie");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CurrentUserService(accessor);

        // Assert
        Assert.True(service.IsAuthenticated);
        Assert.Equal(2, service.UserId);
        Assert.Equal("admintaz", service.Username);
        Assert.True(service.IsAdminTaz);
        Assert.False(service.IsAdminVira);
        Assert.True(service.HasRole(4));
    }

    [Fact]
    public void WhenUserHasMultipleRoles_ShouldExtractAllDistinctRoles()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "8"),
            new Claim(ClaimTypes.Name, "ebrahimi"),
            new Claim(ClaimTypes.Role, "2"),
            new Claim(ClaimTypes.Role, "4"),
            new Claim(ClaimTypes.Role, "6"),
            new Claim("DefaultRole", "4")
        };
        var identity = new ClaimsIdentity(claims, "Cookie");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var service = new CurrentUserService(accessor);

        // Assert
        Assert.True(service.IsAuthenticated);
        Assert.Equal(8, service.UserId);
        Assert.Equal(3, service.RoleIds.Count);
        Assert.True(service.HasRole(2));
        Assert.True(service.HasRole(4));
        Assert.True(service.HasRole(6));
        Assert.False(service.HasRole(5));
    }
}
