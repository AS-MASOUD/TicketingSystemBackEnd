using System.Reflection;
using Carter;
using Microsoft.AspNetCore.Authentication.Cookies;
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

builder.Services.AddAuthorization();

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
builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(assembly));
builder.Services.AddCarter(new DependencyContextAssemblyCatalog(assembly));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCors("TicketingCorsPolicy");

app.UseAuthentication();
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
