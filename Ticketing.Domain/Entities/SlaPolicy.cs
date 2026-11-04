using Ticketing.Domain.Common;

namespace Ticketing.Domain.Entities;

public class SlaPolicy : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; } = 0; // 0 = All, 1 = High, 2 = Medium, 3 = Low
    public int ResponseTimeHours { get; set; } = 4;
    public int ResolutionTimeHours { get; set; } = 24;
    public int? ProjectId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
}
