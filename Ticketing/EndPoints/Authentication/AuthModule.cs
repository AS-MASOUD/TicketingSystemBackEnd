using Carter;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Services;

namespace Ticketing.EndPoints.Authentication;

public record LoginRequest(string? Username, string? Password);
public record ChangePasswordRequest(string? OldPassword, string? NewPassword);
public record RoleDto(int Id, string Name);
public record UserProfileResponse(
    int UserId,
    string Name,
    string Family,
    string Username,
    string Mobile,
    int UserRole,
    IReadOnlyList<RoleDto> UserRoleList,
    IReadOnlyList<string>? Permissions = null
);

public class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/auth")
            .WithTags("Authentication");

        // 1. ورود (Login)
        group.MapPost("login", async (
            [FromBody] LoginRequest request,
            IUserService userService,
            IPermissionService permissionService,
            HttpContext httpContext,
            IConfiguration config,
            CancellationToken ct) =>
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "نام کاربری و رمز عبور الزامی است." });
            }

            var user = await userService.GetByUsernameWithRolesAsync(request.Username);
            if (user == null)
            {
                return Results.Json(new { message = "نام کاربری یا رمز عبور اشتباه است." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            if (!user.IsActive)
            {
                return Results.Json(new { message = "حساب کاربری شما غیرفعال شده است." }, statusCode: StatusCodes.Status403Forbidden);
            }

            bool valid = await userService.VerifyPasswordAsync(user, request.Password);
            if (!valid)
            {
                return Results.Json(new { message = "نام کاربری یا رمز عبور اشتباه است." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            // ایجاد ادعاها (Claims)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("DefaultRole", user.DefaultRoleId.ToString())
            };

            foreach (var role in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                AllowRefresh = true
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties
            );

            var roles = user.UserRoles
                .Select(r => new RoleDto(r.RoleId, r.RoleName))
                .ToList();

            var response = new UserProfileResponse(
                UserId: user.Id,
                Name: user.Name ?? "",
                Family: user.Family ?? "",
                Username: user.Username,
                Mobile: user.Mobile ?? "",
                UserRole: user.DefaultRoleId,
                UserRoleList: roles,
                Permissions: await permissionService.GetUserPermissionsAsync(user.Id)
            );

            return Results.Ok(response);
        })
        .AllowAnonymous()
        .WithOpenApi()
        .Produces<UserProfileResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // 2. استعلام هویت کاربر جاری (Me)
        group.MapGet("me", async (
            IUserService userService,
            IPermissionService permissionService,
            ICurrentUserService currentUser,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
            {
                return Results.Unauthorized();
            }

            var user = await userService.GetByIdWithRolesAsync(currentUser.UserId.Value);
            if (user == null || !user.IsActive)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Unauthorized();
            }

            var roles = user.UserRoles
                .Select(r => new RoleDto(r.RoleId, r.RoleName))
                .ToList();

            var response = new UserProfileResponse(
                UserId: user.Id,
                Name: user.Name ?? "",
                Family: user.Family ?? "",
                Username: user.Username,
                Mobile: user.Mobile ?? "",
                UserRole: user.DefaultRoleId,
                UserRoleList: roles,
                Permissions: await permissionService.GetUserPermissionsAsync(user.Id)
            );

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithOpenApi()
        .Produces<UserProfileResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        // 3. خروج (Logout)
        group.MapPost("logout", async (
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { success = true, message = "با موفقیت خارج شدید." });
        })
        .RequireAuthorization()
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK);

        // 4. تغییر رمز عبور (Change Password)
        group.MapPost("change-password", async (
            [FromBody] ChangePasswordRequest request,
            IUserService userService,
            IPermissionService permissionService,
            ICurrentUserService currentUser,
            CancellationToken ct) =>
        {
            if (!currentUser.UserId.HasValue || string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Results.BadRequest(new { message = "رمز عبور فعلی و جدید الزامی است." });
            }

            if (request.NewPassword.Length < 6)
            {
                return Results.BadRequest(new { message = "طول رمز عبور جدید باید حداقل ۶ کاراکتر باشد." });
            }

            bool changed = await userService.ChangePasswordAsync(currentUser.UserId.Value, request.OldPassword, request.NewPassword);
            if (!changed)
            {
                return Results.BadRequest(new { message = "رمز عبور فعلی اشتباه است." });
            }

            return Results.Ok(new { success = true, message = "رمز عبور با موفقیت تغییر یافت." });
        })
        .RequireAuthorization()
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
