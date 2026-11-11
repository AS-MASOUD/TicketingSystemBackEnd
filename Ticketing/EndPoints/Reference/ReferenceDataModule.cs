using Carter;
using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Enums;
using Ticketing.Infrastructure.Database;
using Ticketing.Services;

namespace Ticketing.EndPoints.Reference;

public sealed class ReferenceDataModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/reference-data", async (TicketingDbContext db, ICurrentUserService user, CancellationToken ct) => {
            var projects = await db.Projects.AsNoTracking().Select(p => new { p.Id, p.Name }).ToListAsync(ct);
            var statuses = await db.Statuses.AsNoTracking().Select(s => new { s.Id, s.Name }).ToListAsync(ct);
            var roles = await db.Roles.AsNoTracking().Where(r => r.IsActive)
                .Select(r => new { r.Id, Name = r.DisplayName }).ToListAsync(ct);
            // This is the existing public legacy developer catalog, not the private login-user directory.
            // Legacy developer keys cannot be replaced with login-user IDs without an explicit data migration.
            var developers = DeveloperCatalog.Active;
            return Results.Ok(new { projects, statuses, roles, developers });
        }).RequireAuthorization().WithTags("Reference");
    }
}
