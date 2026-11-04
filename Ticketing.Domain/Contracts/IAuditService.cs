using Ticketing.Domain.Entities;

namespace Ticketing.Domain.Contracts;

public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? entityId = null, string? details = null, int? actorUserId = null, string? actorUsername = null, string? ipAddress = null);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetLogsAsync(int pageNumber, int pageSize, string? action = null, string? actorUsername = null, DateTime? fromDate = null, DateTime? toDate = null);
}
