using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Security;
using Ticketing.Application.Service.User;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;
using Xunit;

namespace Ticketing.Tests;

[Collection("Restored database")]
public class SecurityAndAuthTests
{
    private const string ConnectionString = "Data Source=.;Initial Catalog=Ticketing_Dev_Restored;Integrated Security=True;TrustServerCertificate=True;";

    private TicketingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new TicketingDbContext(options);
    }

    private static readonly string[] AllowedExtensions = new[]
    {
        ".png", ".jpg", ".jpeg", ".gif", ".pdf", ".zip", ".rar", ".txt", ".xlsx", ".xls", ".docx", ".doc"
    };

    [Theory]
    [InlineData("test.png", true)]
    [InlineData("document.pdf", true)]
    [InlineData("archive.zip", true)]
    [InlineData("data.xlsx", true)]
    [InlineData("malicious.exe", false)]
    [InlineData("script.sh", false)]
    [InlineData("payload.php", false)]
    [InlineData("shell.jsp", false)]
    [InlineData("eval.js", false)]
    public void FileExtension_ShouldOnlyAllowWhitelistedTypes(string fileName, bool isAllowed)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        bool result = AllowedExtensions.Contains(ext);
        Assert.Equal(isAllowed, result);
    }

    [Theory]
    [InlineData("../../../Windows/System32/cmd.exe", "cmd.exe")]
    [InlineData("..\\..\\secret.txt", "secret.txt")]
    [InlineData("normal_file.pdf", "normal_file.pdf")]
    [InlineData("folder/subfolder/image.png", "image.png")]
    public void PathTraversal_ShouldBeStrippedByGetFileName(string inputPath, string expectedSafeName)
    {
        string safeName = Path.GetFileName(inputPath);
        Assert.Equal(expectedSafeName, safeName);
        Assert.DoesNotContain("/", safeName);
        Assert.DoesNotContain("\\", safeName);
        Assert.DoesNotContain("..", safeName);
    }

    [Fact]
    public async Task UserService_VerifyPassword_ShouldValidateAgainstHash()
    {
        using var context = CreateDbContext();
        var userService = new UserService(context);

        var adminVira = await userService.GetByUsernameWithRolesAsync("adminvira");
        Assert.NotNull(adminVira);

        bool valid = await userService.VerifyPasswordAsync(adminVira, "Admin@Vira#2026");
        Assert.True(valid);

        bool invalid = await userService.VerifyPasswordAsync(adminVira, "WrongPass123");
        Assert.False(invalid);
    }

    [Fact]
    public async Task UserService_EnsureSeedUsersAsync_ShouldBeIdempotent()
    {
        using var context = CreateDbContext();
        var userService = new UserService(context);

        // Run seed user creation again to ensure it doesn't fail or duplicate
        await userService.EnsureSeedUsersAsync();

        var adminTaz = await userService.GetByUsernameWithRolesAsync("admintaz");
        Assert.NotNull(adminTaz);
        Assert.Equal(2, adminTaz.Id); // Historical legacy ID preserved
        Assert.Equal(4, adminTaz.DefaultRoleId);

        var mrNadaf = await userService.GetByUsernameWithRolesAsync("mrnadaf");
        Assert.NotNull(mrNadaf);
        Assert.Equal(14, mrNadaf.Id); // Historical legacy ID preserved
        Assert.Equal(2, mrNadaf.DefaultRoleId);
    }
}
