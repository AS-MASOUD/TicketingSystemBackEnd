using Ticketing.Domain.Models;

namespace Ticketing.Domain.Contracts;

public interface IAdminAnalyticsService
{
    Task<AdminOverviewDto> GetOverviewStatsAsync(DateTime? fromDate = null, DateTime? toDate = null, int? projectId = null);
    Task<IReadOnlyList<TicketTrendDto>> GetTicketTrendsAsync(int days = 30, int? projectId = null);
    Task<IReadOnlyList<StatusDistributionDto>> GetStatusDistributionAsync(int? projectId = null);
    Task<IReadOnlyList<PriorityDistributionDto>> GetPriorityDistributionAsync(int? projectId = null);
    Task<SlaPerformanceDto> GetSlaPerformanceAsync(int? projectId = null);
    Task<IReadOnlyList<ProjectWorkloadDto>> GetProjectWorkloadAsync();
    Task<IReadOnlyList<TeamPerformanceDto>> GetTeamPerformanceAsync();
}
