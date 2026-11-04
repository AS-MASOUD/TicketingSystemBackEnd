using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Contracts;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Database;

namespace Ticketing.Application.Service.Admin;

public class AuditService : IAuditService
{
    private readonly TicketingDbContext _dbContext;

    public AuditService(TicketingDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task LogAsync(string action, string entityName, string? entityId = null, string? details = null, int? actorUserId = null, string? actorUsername = null, string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                ActorUserId = actorUserId,
                ActorUsername = actorUsername,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.AuditLogs.AddAsync(log);
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            // Logging should never crash business operations
        }
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetLogsAsync(int pageNumber, int pageSize, string? action = null, string? actorUsername = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action.Contains(action));

        if (!string.IsNullOrWhiteSpace(actorUsername))
            query = query.Where(l => l.ActorUsername != null && l.ActorUsername.Contains(actorUsername));

        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);

        int totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
