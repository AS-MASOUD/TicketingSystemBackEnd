using System.Reflection;
using System.Security.Claims;
using Carter;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Service.Massage;
using Ticketing.Application.Service.Project;
using Ticketing.Application.Service.Stauts;
using Ticketing.Application.Service.Ticket;
using Ticketing.Application.Service.TicketFlow;
using Ticketing.Application.Service.User;
using Ticketing.Application.Service.Admin;
using Ticketing.Domain.Contracts;
using Ticketing.EndPoints.Reporting.Query.Export;
using Ticketing.Infrastructure.Database;
using Ticketing.Services;

var builder = WebApplication.CreateBuilder(args);

// Forwarded headers for IIS / ARR reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("TicketingCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// SQL Server DbContext
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<TicketingDbContext>(options => options.UseSqlServer(connectionString));

// Data Protection persistence across application restarts
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("SFS.Ticketing");

// Cookie Authentication
string cookieName = builder.Configuration["Auth:CookieName"] ?? "SFS.Ticketing.Auth";
string cookiePath = builder.Configuration["Auth:CookiePath"] ?? "/ticketing";

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = cookieName;
        options.Cookie.Path = cookiePath;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out int id))
                { context.RejectPrincipal(); return; }
                var userService = context.HttpContext.RequestServices.GetRequiredService<IUserService>();
                var user = await userService.GetByIdWithRolesAsync(id);
                if (user == null || !user.IsActive)
                { context.RejectPrincipal(); return; }
                var db = context.HttpContext.RequestServices.GetRequiredService<TicketingDbContext>();
                var assigned = user.UserRoles.Select(r => r.RoleId).ToArray();
                var activeRoles = await db.Roles.Where(r => r.IsActive && assigned.Contains(r.Id)).Select(r => r.Id).ToListAsync();
                if (activeRoles.Count == 0)
                { context.RejectPrincipal(); return; }
                var defaultRole = activeRoles.Contains(user.DefaultRoleId) ? user.DefaultRoleId : activeRoles[0];
                var claims = new List<Claim> {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Name, user.Username),
                    new("DefaultRole", defaultRole.ToString())
                };
                claims.AddRange(activeRoles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));
                var previous = context.Principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).Order().ToArray();
                context.ShouldRenew = !previous.SequenceEqual(activeRoles.Select(r => r.ToString()).Order()) ||
                    context.Principal.FindFirstValue("DefaultRole") != defaultRole.ToString();
                context.ReplacePrincipal(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            },
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

// Application Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectRoleService, ProjectRoleService>();
builder.Services.AddScoped<IMassageService, MassageService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITicketFlowService, TicketFlowService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
builder.Services.AddScoped<IAdminManagementService, AdminManagementService>();
builder.Services.AddSingleton<IExport, ExportService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var assembly = Assembly.GetExecutingAssembly();
builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssembly(assembly);
    configuration.AddOpenBehavior(typeof(CallerIdentityBehavior<,>));
    configuration.AddOpenBehavior(typeof(TicketNotificationBehavior<,>));
});
builder.Services.AddCarter(new DependencyContextAssemblyCatalog(assembly));

var app = builder.Build();

app.UseForwardedHeaders();

// Normalize URL paths to prevent 404 from IIS rewrite variants or duplicate prefixes
app.Use((context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/ticketing/api/", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Path = path.Substring("/ticketing".Length);
    }
    else if (path.Equals("/ticketing/api", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Path = "/api";
    }
    else if (path.StartsWith("/api/api/", StringComparison.OrdinalIgnoreCase))
    {
        context.Request.Path = path.Substring("/api".Length);
    }
    return next();
});

app.UseRouting();
app.UseCors("TicketingCorsPolicy");

app.UseAuthentication();
app.UseMiddleware<CallerIdentityExceptionMiddleware>();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();
app.MapControllers();
app.MapCarter();

// Seed initial users if needed
using (var scope = app.Services.CreateScope())
{
    try
    {
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        await userService.EnsureSeedUsersAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Could not seed default users on startup.");
    }
}

app.Run();
